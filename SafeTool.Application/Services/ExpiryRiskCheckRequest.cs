namespace SafeTool.Application.Services;

public class ExpiryRiskCheckRequest
{
    public double T1 { get; set; }
    public double T10D { get; set; }
    public DateTime? LastTestDate { get; set; }
}

