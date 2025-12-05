namespace SafeTool.Application.Services;

public class DualStandardEvaluationRequest
{
    public SafeTool.Domain.Compliance.ComplianceChecklist Iso13849Checklist { get; set; } = new();
    public SafeTool.Domain.Standards.SafetyFunction62061 Iec62061Function { get; set; } = new();
    public DualStandardEvaluationOptions? Options { get; set; }
}

