namespace GenAI_Insurance.Server.Services;

public class MemoryService
{
    private readonly Dictionary<string, List<string>> _mems = new();

    public void Save(string userId, string message)
    {
        if (!_mems.ContainsKey(userId)) _mems[userId] = new List<string>();
        _mems[userId].Add(message);
    }

    public IEnumerable<string> Get(string userId) => _mems.TryGetValue(userId, out var list) ? list : Enumerable.Empty<string>();
}
