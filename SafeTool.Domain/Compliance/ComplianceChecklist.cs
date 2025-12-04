using SafeTool.Domain.Standards;

namespace SafeTool.Domain.Compliance;

public class ComplianceChecklist
{
    public ISO12100Assessment ISO12100 { get; set; } = new();
    public ISO13849Assessment ISO13849 { get; set; } = new();
    public List<ChecklistItem> GeneralItems { get; set; } = new();
    public string SystemName { get; set; } = string.Empty;
    public string? ProjectId { get; set; }
    public string Assessor { get; set; } = string.Empty;
    public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;
}

