namespace GenAI_Insurance.Server.Models;

public class DocumentChunk
{
    public int Id { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public int ChunkNumber { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public float[]? Embedding { get; set; }
    public string? Topic { get; set; }
    public System.DateTimeOffset CreatedDate { get; set; }
}
