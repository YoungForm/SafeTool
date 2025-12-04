namespace SafeTool.Domain.Interop;

public class ProjectDto
{
    public MetaDto Meta { get; set; } = new();
    public List<SafeTool.Domain.Standards.SafetyFunction62061> Functions { get; set; } = new();
}

public class MetaDto
{
    public string Name { get; set; } = string.Empty;
    public string Standard { get; set; } = "IEC62061";
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

