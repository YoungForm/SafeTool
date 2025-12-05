namespace SafeTool.Application.Services;

public class ImportComponentsRequest
{
    public string Json { get; set; } = string.Empty;
    public ImportOptions? Options { get; set; }
}

