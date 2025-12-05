namespace SafeTool.Application.Services;

public class ElectricalDrawingLinkRequest
{
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public ElectricalDrawingInfo Drawing { get; set; } = new();
}

