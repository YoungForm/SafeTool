namespace SafeTool.Application.Services;

public class ImportComponentsCsvRequest
{
    public string Csv { get; set; } = string.Empty;
    public ImportOptions? Options { get; set; }
}

