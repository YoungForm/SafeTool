namespace SafeTool.Application.Services;

public class ApproveRequest
{
    public string? Comment { get; set; }
    public bool IsFirstReviewer { get; set; } = true;
}

