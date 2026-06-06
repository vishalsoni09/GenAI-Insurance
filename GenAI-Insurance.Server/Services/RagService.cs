namespace GenAI_Insurance.Server.Services;

public class RagService
{
    private readonly Dictionary<string,string> _docs = new();

    public RagService()
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "Documents");
        if (Directory.Exists(basePath))
        {
            foreach (var f in Directory.GetFiles(basePath, "*.txt"))
            {
                _docs[Path.GetFileName(f)] = File.ReadAllText(f);
            }
        }
    }

    public IEnumerable<string> Query(string q)
    {
        var low = q.ToLowerInvariant();
        foreach (var kv in _docs)
        {
            if (kv.Value.ToLowerInvariant().Contains(low)) yield return kv.Value;
        }
    }
}
