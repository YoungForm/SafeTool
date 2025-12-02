namespace SafeTool.Domain.SRS;

public class SrsDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SystemName { get; set; } = string.Empty;
    public string Assessor { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public string Version { get; set; } = "v1.0";
    public string Status { get; set; } = "Draft"; // Draft/UnderReview/Approved

    // 关键参数
    public string OperatingModes { get; set; } = string.Empty;
    public string SafetyFunction { get; set; } = string.Empty;
    public string RequiredPLr { get; set; } = string.Empty;
    public string ArchitectureCategory { get; set; } = string.Empty;
    public double DCavg { get; set; }
    public double MTTFd { get; set; }
    public string ReactionTime { get; set; } = string.Empty;
    public string SafeState { get; set; } = string.Empty;
    public string DiagnosticsStrategy { get; set; } = string.Empty;
    public string IOMap { get; set; } = string.Empty;
    public string EnvironmentalRequirements { get; set; } = string.Empty;
    public string EMCRequirements { get; set; } = string.Empty;
    public string MaintenanceTesting { get; set; } = string.Empty;
    public string CCFMeasures { get; set; } = string.Empty;

    public List<SrsRequirement> Requirements { get; set; } = new();
}

