namespace GenAI_Insurance.Server.Services;

using System;
using System.Collections.Generic;
using System.Linq;

public class MemoryService
{
    private readonly Dictionary<string, List<Models.ChatMessage>> _mems = new();
    // track current topic per session
    private readonly Dictionary<string, string?> _topic = new();

    private static string NormalizeSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return "default";
        return sessionId!;
    }

    // Backwards-compatible signature
    public void AddMessage(string sessionId, string role, string message)
        => AddMessage(sessionId, role, message, null);

    // Structured add - optional topic
    public void AddMessage(string? sessionId, string role, string message, string? topic)
    {
        var sid = NormalizeSession(sessionId);
        if (!_mems.ContainsKey(sid)) _mems[sid] = new List<Models.ChatMessage>();
        var msg = new Models.ChatMessage
        {
            Role = role,
            Content = message,
            Timestamp = DateTimeOffset.UtcNow,
            Topic = topic
        };
        _mems[sid].Add(msg);
        if (!string.IsNullOrWhiteSpace(topic)) _topic[sid] = topic;
    }

    // Return history as simple strings for backward compatibility
    public IEnumerable<string> GetHistory(string? sessionId)
    {
        var sid = NormalizeSession(sessionId);
        if (!_mems.TryGetValue(sid, out var list)) return Enumerable.Empty<string>();
        return list.Select(m => $"{m.Role}: {m.Content}");
    }

    // New: return structured messages
    public IEnumerable<Models.ChatMessage> GetMessages(string? sessionId)
    {
        var sid = NormalizeSession(sessionId);
        if (!_mems.TryGetValue(sid, out var list)) return Enumerable.Empty<Models.ChatMessage>();
        return list;
    }

    // Get last N messages (most recent last)
    public IEnumerable<Models.ChatMessage> GetRecentMessages(string? sessionId, int maxMessages = 10)
    {
        var sid = NormalizeSession(sessionId);
        if (!_mems.TryGetValue(sid, out var list) || list.Count == 0) return Enumerable.Empty<Models.ChatMessage>();
        return list.Skip(Math.Max(0, list.Count - maxMessages));
    }

    // Topic helpers
    public string? GetTopic(string? sessionId)
    {
        var sid = NormalizeSession(sessionId);
        return _topic.TryGetValue(sid, out var t) ? t : null;
    }

    public void SetTopic(string? sessionId, string topic)
    {
        var sid = NormalizeSession(sessionId);
        _topic[sid] = topic;
    }

    public void ClearTopic(string? sessionId)
    {
        var sid = NormalizeSession(sessionId);
        if (_topic.ContainsKey(sid)) _topic.Remove(sid);
    }

    // Build messages for OpenAI prompt: system + recent chat messages
    public List<object> BuildMessagesForPrompt(string? sessionId, string systemPrompt, int recent = 12)
    {
        var msgs = new List<object>();
        msgs.Add(new { role = "system", content = systemPrompt });
        var recentMsgs = GetRecentMessages(sessionId, recent);
        foreach (var m in recentMsgs)
        {
            var role = string.IsNullOrWhiteSpace(m.Role) ? "user" : m.Role.ToLowerInvariant();
            msgs.Add(new { role = role, content = m.Content });
        }
        return msgs;
    }

    // backward compatible alias
    public IEnumerable<string> Get(string? userId) => GetHistory(userId);
}
