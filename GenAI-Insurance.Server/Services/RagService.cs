namespace GenAI_Insurance.Server.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenAI_Insurance.Server.Models;

public class RagService
{
    private readonly IWebHostEnvironment _env;
    private readonly OpenAIService _openAIService;
    private readonly List<DocumentChunk> _chunks = new();
    private bool _indexBuilt = false;
    private readonly object _lock = new();

    public RagService(IWebHostEnvironment env, OpenAIService openAIService)
    {
        _env = env;
        _openAIService = openAIService;
    }

    // Public method to trigger a rebuild of the in-memory embedding index.
    // Returns number of chunks indexed.
    public async Task<int> RebuildIndexAsync()
    {
        // allow rebuild even if previously marked built
        lock (_lock)
        {
            _indexBuilt = false;
        }

        _chunks.Clear();
        await BuildIndexAsync();

        lock (_lock)
        {
            _indexBuilt = true;
        }

        return _chunks.Count;
    }

    // Async version that builds embeddings and performs vector similarity search.
    public async Task<string> GetRelevantDocumentAsync(string question, IEnumerable<string>? conversationHistory = null)
    {
        // Ensure index
        if (!_indexBuilt)
        {
            lock (_lock)
            {
                if (!_indexBuilt)
                {
                    // build in background without awaiting here
                    Task.Run(async () => await BuildIndexAsync()).ConfigureAwait(false);
                    _indexBuilt = true; // mark to avoid rebuilding
                }
            }
        }

        // Try embedding-based search
        var qEmb = await _openAIService.GetEmbeddingAsync(question);
        if (qEmb != null && _chunks.Count > 0 && _chunks.Any(c => c.Embedding != null))
        {
            // compute cosine similarities
            var scored = new List<(DocumentChunk Chunk, double Score)>();
            foreach (var c in _chunks)
            {
                if (c.Embedding == null) continue;
                var score = CosineSimilarity(qEmb, c.Embedding);
                scored.Add((c, score));
            }
            var top = scored.OrderByDescending(s => s.Score).Take(3).Select(s => s.Chunk.ChunkText).ToArray();
            if (top.Length > 0) return string.Join("\n\n", top);
        }

        // fallback to keyword-based article selection (existing behavior)
        return GetRelevantDocumentFallback(question, conversationHistory);
    }

    // Synchronous wrapper for existing callers
    public string GetRelevantDocument(string question, IEnumerable<string>? conversationHistory = null)
    {
        return GetRelevantDocumentAsync(question, conversationHistory).GetAwaiter().GetResult();
    }

    public IEnumerable<string> Query(string q, IEnumerable<string>? conversationHistory = null)
    {
        yield return GetRelevantDocument(q, conversationHistory);
    }

    private async Task BuildIndexAsync()
    {
        try
        {
            var dataFolder = Path.Combine(_env.ContentRootPath, "Documents");
            if (!Directory.Exists(dataFolder)) return;
            var files = Directory.GetFiles(dataFolder, "*.txt");
            int id = 1;
            foreach (var file in files)
            {
                var docName = Path.GetFileName(file);
                var text = await File.ReadAllTextAsync(file);
                var passages = SplitIntoPassages(text, 800).ToArray();
                for (int i = 0; i < passages.Length; i++)
                {
                    var chunk = new DocumentChunk
                    {
                        Id = id++,
                        DocumentName = docName,
                        ChunkNumber = i + 1,
                        ChunkText = passages[i],
                        CreatedDate = DateTimeOffset.UtcNow
                    };
                    // compute embedding
                    try
                    {
                        var emb = await _openAIService.GetEmbeddingAsync(chunk.ChunkText);
                        chunk.Embedding = emb;
                    }
                    catch { /* ignore embedding failures per-chunk */ }
                    _chunks.Add(chunk);
                }
            }
        }
        catch { /* silent */ }
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        if (na == 0 || nb == 0) return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    // Fallback keyword-based selection (previous behavior)
    private string GetRelevantDocumentFallback(string question, IEnumerable<string>? conversationHistory)
    {
        var q = (question ?? string.Empty).ToLowerInvariant();
        var dataFolder = Path.Combine(_env.ContentRootPath, "Documents");
        string fileName = "health_policy.txt";
        if (conversationHistory != null)
        {
            foreach (var item in conversationHistory.Reverse())
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                var s = item.ToLowerInvariant();
                if (s.Contains("car") || s.Contains("vehicle") || s.Contains("motor") || s.Contains("auto")) { fileName = "car_policy.txt"; break; }
                if (s.Contains("health") || s.Contains("medical")) { fileName = "health_policy.txt"; break; }
                if (s.Contains("life")) { fileName = "life_policy.txt"; break; }
            }
        }
        if (fileName == "health_policy.txt")
        {
            if (q.Contains("life")) fileName = "life_policy.txt";
            else if (q.Contains("car") || q.Contains("vehicle") || q.Contains("motor") || q.Contains("auto")) fileName = "car_policy.txt";
            else if (q.Contains("health") || q.Contains("medical")) fileName = "health_policy.txt";
        }
        var path = Path.Combine(dataFolder, fileName);
        if (!File.Exists(path)) return "No policy document found.";
        var text = File.ReadAllText(path);
        var passages = SplitIntoPassages(text, 800).ToArray();
        if (passages.Length == 0) return text;
        var queryTerms = Tokenize(q).Distinct().Where(t => t.Length > 1).ToArray();
        if (queryTerms.Length == 0) return string.Join("\n\n", passages.Take(3));
        var scores = new List<(string Passage, int Score)>();
        foreach (var p in passages)
        {
            var pLow = p.ToLowerInvariant();
            var score = 0;
            foreach (var t in queryTerms) if (pLow.Contains(t)) score++;
            scores.Add((p, score));
        }
        var top = scores.OrderByDescending(s => s.Score).ThenBy(s => s.Passage.Length).Where(s => s.Score > 0).Take(3).Select(s => s.Passage).ToArray();
        if (top.Length == 0) return string.Join("\n\n", passages.Take(2));
        return string.Join("\n\n", top);
    }

    private static IEnumerable<string> SplitIntoPassages(string text, int approxMaxChars)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        var parts = text.Split(new[] {"\r\n\r\n", "\n\n"}, StringSplitOptions.RemoveEmptyEntries);
        var buffer = new StringBuilder();
        foreach (var p in parts)
        {
            var trimmed = p.Trim();
            if (trimmed.Length == 0) continue;
            if (buffer.Length + trimmed.Length + 2 <= approxMaxChars)
            {
                if (buffer.Length > 0) buffer.Append("\n\n");
                buffer.Append(trimmed);
            }
            else
            {
                if (buffer.Length > 0) { yield return buffer.ToString(); buffer.Clear(); }
                if (trimmed.Length <= approxMaxChars) yield return trimmed;
                else
                {
                    var sentences = trimmed.Split(new[] {'.', '?', '!'}, StringSplitOptions.RemoveEmptyEntries);
                    var sb = new StringBuilder();
                    foreach (var s in sentences)
                    {
                        var seg = s.Trim();
                        if (seg.Length == 0) continue;
                        if (sb.Length + seg.Length + 2 <= approxMaxChars) { if (sb.Length>0) sb.Append('.'); sb.Append(seg); }
                        else { if (sb.Length>0) { yield return sb.ToString() + "."; sb.Clear(); } if (seg.Length <= approxMaxChars) sb.Append(seg); }
                    }
                    if (sb.Length>0) yield return sb.ToString() + ".";
                }
            }
        }
        if (buffer.Length > 0) yield return buffer.ToString();
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        var cleaned = new string(text.Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray());
        foreach (var tok in cleaned.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)) yield return tok;
    }
}
