namespace LoanEligibility.Poc.Api.Services;

public class RagService
{
    // For POC, load text files from Documents folder and do naive keyword search
    private readonly Dictionary<string,string> _docs = new();

    public RagService()
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Documents");
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
