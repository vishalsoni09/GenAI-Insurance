using System;
using System.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Linq;
using GenAI_Insurance.Server.Models;
using Microsoft.Extensions.Logging;

namespace GenAI_Insurance.Server.Services;

public class OpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<OpenAIService> _logger;
    // cache commonly-used AzureOpenAI settings
    private readonly string? _endpoint;
    private readonly string? _endpointNormalized;
    private readonly string? _apiKey;
    private readonly string? _deploymentName;
    private readonly string _apiVersion;
    private readonly string? _embeddingModel;
    private readonly string? _embeddingDeploymentName;
    private readonly bool _isFoundryOrOpenAIv1;

    public OpenAIService(HttpClient httpClient, IConfiguration config, ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _endpoint = _config["AzureOpenAI:Endpoint"]?.TrimEnd('/');
        _endpointNormalized = _endpoint?.TrimEnd('/');
        _apiKey = _config["AzureOpenAI:ApiKey"];
        _deploymentName = _config["AzureOpenAI:DeploymentName"];
        _apiVersion = _config["AzureOpenAI:ApiVersion"] ?? "2024-10-21";
        _embeddingModel = _config["AzureOpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        _embeddingDeploymentName = _config["AzureOpenAI:EmbeddingDeploymentName"] ?? null;
        _isFoundryOrOpenAIv1 = (_endpointNormalized ?? string.Empty).Contains("/openai/v1", StringComparison.OrdinalIgnoreCase)
            || (_endpointNormalized ?? string.Empty).Contains("services.ai.azure.com", StringComparison.OrdinalIgnoreCase);
    }

    private bool ResolveUseBearer()
    {
        if (!string.IsNullOrWhiteSpace(_config["AzureOpenAI:UseBearer"]))
        {
            if (bool.TryParse(_config["AzureOpenAI:UseBearer"], out var v)) return v;
        }
        // default to bearer for Foundry / services.ai endpoints
        return (_endpointNormalized ?? string.Empty).Contains("services.ai.azure.com", StringComparison.OrdinalIgnoreCase)
               || (_endpointNormalized ?? string.Empty).Contains("/openai/v1", StringComparison.OrdinalIgnoreCase);
    }

    // Compute embedding for a single input using text-embedding-3-small by default
    public async Task<float[]?> GetEmbeddingAsync(string input, string? modelOverride = null)
    {
        var endpoint = _endpoint;
        var apiKey = _apiKey;
        var apiVersion = _apiVersion;
        var embeddingModel = modelOverride ?? _embeddingModel;

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger?.LogWarning("Embedding request skipped: missing endpoint or api key");
            return null;
        }

        var isFoundryOrOpenAIv1 = _isFoundryOrOpenAIv1;

        string url;
        object payload;
        var normalized = _endpointNormalized ?? endpoint.TrimEnd('/');
        if (isFoundryOrOpenAIv1)
        {
            url = normalized.EndsWith("/openai/v1", StringComparison.OrdinalIgnoreCase) ? normalized + "/embeddings" : normalized + "/openai/v1/embeddings";
            payload = new { model = embeddingModel, input };
        }
        else
        {
            // For Azure deployments: use EmbeddingDeploymentName config if present, otherwise fall back to embeddingModel
            var embDeployment = _config["AzureOpenAI:EmbeddingDeploymentName"] ?? embeddingModel;
            url = $"{endpoint}/openai/deployments/{embDeployment}/embeddings?api-version={apiVersion}";
            payload = new { input };
        }

        bool useBearer = ResolveUseBearer();

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        if (useBearer) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        else req.Headers.Add("api-key", apiKey);
        var json = JsonSerializer.Serialize(payload);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var resp = await _httpClient.SendAsync(req);
            var content = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Embedding request failed {Status}: {Content}", resp.StatusCode, content);
                return null;
            }
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            // data[0].embedding
            if (root.TryGetProperty("data", out var dataEl) && dataEl.GetArrayLength() > 0)
            {
                var embEl = dataEl[0].GetProperty("embedding");
                var list = new List<float>();
                foreach (var v in embEl.EnumerateArray()) list.Add(v.GetSingle());
                return list.ToArray();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception while fetching embedding");
            return null;
        }
    }

    // Return raw deployments list from the Azure OpenAI resource to help diagnostics
    public async Task<string> ListDeploymentsRawAsync()
    {
        var endpoint = _endpoint;
        var apiKey = _apiKey;
        var apiVersion = _apiVersion;
        bool useBearer = ResolveUseBearer();

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            return "Azure OpenAI configuration is missing (Endpoint or ApiKey).";
        }

        var url = $"{endpoint}/openai/deployments?api-version={apiVersion}";

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (useBearer)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
            else
            {
                req.Headers.Add("api-key", apiKey);
            }
            using var resp = await _httpClient.SendAsync(req);
            var content = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                return $"Error fetching deployments: {resp.StatusCode} - {content}";
            }
            return content;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception while listing deployments");
            return $"Exception while listing deployments: {ex.Message}";
        }
    }

    // Synchronous wrapper used by existing code (POC). It blocks on the async call.
    public Models.OpenAIResponse Reply(string message)
    {
        return GetChatCompletionRawAsync("", message).GetAwaiter().GetResult();
    }

    public (string Sql, Models.OpenAIResponse Metadata) GenerateSql(string question)
    {
        var system = "You are an assistant that generates SQL SELECT statements only. Return a single valid T-SQL SELECT statement using dbo schema. Do not include explanatory text.";
        var meta = GetChatCompletionRawAsync(system, question).GetAwaiter().GetResult();
        var sql = meta.Choices?.FirstOrDefault()?.Message?.Content ?? "SELECT 1 as [One]";
        return (sql.Trim(), meta);
    }

    // Call Azure OpenAI Chat Completions endpoint and return parsed response.
    public async Task<Models.OpenAIResponse> GetChatCompletionRawAsync(string systemPrompt, string userPrompt)
    {
        var endpoint = _endpoint;
        var apiKey = _apiKey;
        var deploymentName = _deploymentName;
        var apiVersion = _apiVersion;

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(deploymentName))
        {
            // return a simple fallback response
            var fallback = new Models.OpenAIResponse();
            fallback.Choices.Add(new Models.Choice { Message = new Models.Message { Content = "Azure OpenAI configuration is missing." } });
            return fallback;
        }

        // Support two endpoint styles:
        // - Azure OpenAI (deployments REST): {endpoint}/openai/deployments/{deployment}/chat/completions?api-version=...
        // - Azure Foundry / OpenAI-compatible: {endpoint}/openai/v1/chat/completions with payload.model = deploymentName
        var isFoundryOrOpenAIv1 = _isFoundryOrOpenAIv1;

        string url;
        object payload;

        var normalized = _endpointNormalized ?? endpoint.TrimEnd('/');
        if (isFoundryOrOpenAIv1)
        {
            // use OpenAI-compatible v1 path and include model in payload
            // If endpoint already contains /openai/v1 then append /chat/completions only once
            if (normalized.EndsWith("/openai/v1", StringComparison.OrdinalIgnoreCase))
            {
                url = normalized + "/chat/completions";
            }
            else
            {
                url = normalized + "/openai/v1/chat/completions";
            }
            payload = new
            {
                model = deploymentName,
                messages = new[] {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.2,
                max_tokens = 500
            };
        }
        else
        {
            // default to Azure OpenAI deployments endpoint
            url = $"{endpoint}/openai/deployments/{deploymentName}/chat/completions?api-version={apiVersion}";
            payload = new
            {
                messages = new[] {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.2,
                max_tokens = 500
            };
        }

        bool useBearerChat = ResolveUseBearer();

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (useBearerChat)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        else
        {
            request.Headers.Add("api-key", apiKey);
        }
        var payloadJson = JsonSerializer.Serialize(payload);
        request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

        try
        {
            _logger?.LogInformation("Sending Azure OpenAI request to {Url} with deployment {Deployment}", url, deploymentName);
            _logger?.LogDebug("Payload: {Payload}", payloadJson);

            HttpResponseMessage? response = null;
            string content = string.Empty;
            try
            {
                response = await _httpClient.SendAsync(request);
                content = await response.Content.ReadAsStringAsync();

                // If the service rejects the api-key header with 401, some Foundry endpoints expect Authorization: Bearer
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger?.LogWarning("Initial request unauthorized (api-key). Retrying with Authorization: Bearer");
                    response.Dispose();
                    using var retryReq = new HttpRequestMessage(HttpMethod.Post, url);
                    retryReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    retryReq.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                    response = await _httpClient.SendAsync(retryReq);
                    content = await response.Content.ReadAsStringAsync();
                }

                _logger?.LogInformation("Azure OpenAI response {Status}", response.StatusCode);
                _logger?.LogDebug("Azure OpenAI content: {Content}", content);
            }
            finally
            {
                // Do not dispose here - response is used below and will be disposed later in its scope
            }

            if (!response.IsSuccessStatusCode)
            {
                // Try to parse structured error from Azure OpenAI and return a clearer message
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;
                    string? code = null;
                    string? msg = null;
                    if (root.TryGetProperty("error", out var errEl))
                    {
                        if (errEl.ValueKind == JsonValueKind.Object)
                        {
                            if (errEl.TryGetProperty("code", out var codeEl) && codeEl.ValueKind == JsonValueKind.String)
                                code = codeEl.GetString();
                            if (errEl.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
                                msg = msgEl.GetString();
                        }
                    }

                    var userMessage = $"Azure OpenAI call failed: {response.StatusCode}";
                    if (!string.IsNullOrEmpty(code)) userMessage += $" - {code}";
                    if (!string.IsNullOrEmpty(msg)) userMessage += $": {msg}";
                    userMessage += "\nPlease verify AzureOpenAI:Endpoint, AzureOpenAI:ApiKey and AzureOpenAI:DeploymentName in configuration and that the deployment exists.";

                    _logger?.LogWarning("Azure OpenAI error: {Code} {Message}", code, msg);

                    var err = new Models.OpenAIResponse();
                    err.Choices.Add(new Models.Choice { Message = new Models.Message { Content = userMessage } });
                    return err;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to parse Azure OpenAI error response");
                    var err = new Models.OpenAIResponse();
                    err.Choices.Add(new Models.Choice { Message = new Models.Message { Content = $"Azure OpenAI call failed: {response.StatusCode} - {content}" } });
                    return err;
                }
            }

            try
            {
                var result = JsonSerializer.Deserialize<Models.OpenAIResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result ?? new Models.OpenAIResponse();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to deserialize Azure OpenAI response");
                var err = new Models.OpenAIResponse();
                err.Choices.Add(new Models.Choice { Message = new Models.Message { Content = content } });
                return err;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception while calling Azure OpenAI");
            var err = new Models.OpenAIResponse();
            err.Choices.Add(new Models.Choice { Message = new Models.Message { Content = $"Azure OpenAI request failed: {ex.Message}" } });
            return err;
        }
    }
}
