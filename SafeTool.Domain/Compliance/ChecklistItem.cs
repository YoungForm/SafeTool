namespace SafeTool.Domain.Compliance;

public class ChecklistItem
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
    public bool Completed { get; set; }
    public string? Evidence { get; set; }
}

