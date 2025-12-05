namespace SafeTool.Application.Services;

public class BatchReportGenerationRequest
{
    public List<BatchReportRequest> Requests { get; set; } = new();
    public string? Format { get; set; } = "html";
    public string? Language { get; set; } = "zh-CN";
    public BatchReportOptions? Options { get; set; }
}

