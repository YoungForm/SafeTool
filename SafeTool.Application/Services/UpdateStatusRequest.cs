namespace SafeTool.Application.Services;

public class UpdateStatusRequest
{
    public RemediationStatus Status { get; set; }
    public string? Comment { get; set; }
}

