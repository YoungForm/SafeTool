namespace SafeTool.Domain.Compliance;

public class EvaluationResult
{
    public bool IsCompliant { get; set; }
    public string Summary { get; set; } = string.Empty;
    public Dictionary<string, string> Details { get; set; } = new();
    public List<string> NonConformities { get; set; } = new();
    public string RecommendedActions { get; set; } = string.Empty;
}

