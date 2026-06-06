namespace GenAI_Insurance.Server.Services;

public class DbService
{
    private readonly Dictionary<string, string> _store = new();

    public void Save(string key, string value) => _store[key] = value;
    public string? Get(string key) => _store.TryGetValue(key, out var v) ? v : null;
}
