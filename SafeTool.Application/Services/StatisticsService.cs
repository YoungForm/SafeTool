namespace SafeTool.Application.Services;

/// <summary>
/// 统计报表服务（P2优先级）
/// 生成系统统计报表
/// </summary>
public class StatisticsService
{
    private readonly ComponentLibraryService _componentLibrary;
    private readonly EvidenceService _evidenceService;
    private readonly VerificationChecklistService _checklistService;
    private readonly ComplianceMatrixService _matrixService;

    public StatisticsService(
        ComponentLibraryService componentLibrary,
        EvidenceService evidenceService,
        VerificationChecklistService checklistService,
        ComplianceMatrixService matrixService)
    {
        _componentLibrary = componentLibrary;
        _evidenceService = evidenceService;
        _checklistService = checklistService;
        _matrixService = matrixService;
    }

    /// <summary>
    /// 生成系统统计报表
    /// </summary>
    public SystemStatisticsReport GenerateSystemStatistics(string? projectId = null)
    {
        var report = new SystemStatisticsReport
        {
            GeneratedAt = DateTime.UtcNow,
            ProjectId = projectId
        };

        // 组件统计
        report.ComponentStatistics = GenerateComponentStatistics();

        // 证据统计
        report.EvidenceStatistics = GenerateEvidenceStatistics(projectId);

        // 检查清单统计
        report.ChecklistStatistics = GenerateChecklistStatistics(projectId);

        // 合规矩阵统计
        report.MatrixStatistics = GenerateMatrixStatistics(projectId);

        // 总体统计
        report.OverallStatistics = GenerateOverallStatistics(report);

        return report;
    }

    /// <summary>
    /// 生成组件统计
    /// </summary>
    private ComponentStatistics GenerateComponentStatistics()
    {
        var components = _componentLibrary.List().ToList();
        var categories = components.GroupBy(c => c.Category).ToList();

        return new ComponentStatistics
        {
            TotalCount = components.Count,
            CategoryCount = categories.Count,
            CategoryBreakdown = categories.ToDictionary(
                g => g.Key,
                g => g.Count()),
            ManufacturerCount = components.Select(c => c.Manufacturer).Distinct().Count(),
            Manufacturers = components.GroupBy(c => c.Manufacturer)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <summary>
    /// 生成证据统计
    /// </summary>
    private EvidenceStatistics GenerateEvidenceStatistics(string? projectId)
    {
        var allEvidence = _evidenceService.List(null, null).ToList();
        var now = DateTime.UtcNow;

        return new EvidenceStatistics
        {
            TotalCount = allEvidence.Count,
            ValidCount = allEvidence.Count(e => e.ValidUntil == null || e.ValidUntil > now),
            ExpiredCount = allEvidence.Count(e => e.ValidUntil != null && e.ValidUntil <= now),
            ExpiringSoonCount = allEvidence.Count(e => e.ValidUntil != null && e.ValidUntil > now && e.ValidUntil <= now.AddDays(30)),
            TypeBreakdown = allEvidence.GroupBy(e => e.Type)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <summary>
    /// 生成检查清单统计
    /// </summary>
    private ChecklistStatistics GenerateChecklistStatistics(string? projectId)
    {
        var checklists = _checklistService.List(projectId ?? "").ToList();
        var allItems = checklists.SelectMany(c => c.Items).ToList();

        return new ChecklistStatistics
        {
            ChecklistCount = checklists.Count,
            TotalItemCount = allItems.Count,
            CompletedItemCount = allItems.Count(i => i.Status == "pass"),
            PendingItemCount = allItems.Count(i => i.Status == "pending"),
            FailedItemCount = allItems.Count(i => i.Status == "fail"),
            StandardBreakdown = checklists.GroupBy(c => c.Standard)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <summary>
    /// 生成合规矩阵统计
    /// </summary>
    private MatrixStatistics GenerateMatrixStatistics(string? projectId)
    {
        var allEntries = _matrixService.Get(projectId ?? "").ToList();

        return new MatrixStatistics
        {
            MatrixCount = 1, // 每个项目一个矩阵
            TotalEntryCount = allEntries.Count,
            CompliantEntryCount = allEntries.Count(e => e.Result == "Compliant"),
            NonCompliantEntryCount = allEntries.Count(e => e.Result == "NonCompliant"),
            PendingEntryCount = allEntries.Count(e => e.Result == "Pending"),
            StandardBreakdown = allEntries.GroupBy(e => e.Standard)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <summary>
    /// 生成总体统计
    /// </summary>
    private OverallStatistics GenerateOverallStatistics(SystemStatisticsReport report)
    {
        return new OverallStatistics
        {
            TotalComponents = report.ComponentStatistics.TotalCount,
            TotalEvidence = report.EvidenceStatistics.TotalCount,
            TotalChecklists = report.ChecklistStatistics.ChecklistCount,
            TotalMatrices = report.MatrixStatistics.MatrixCount,
            ComplianceRate = report.MatrixStatistics.MatrixCount > 0
                ? (double)report.MatrixStatistics.CompliantEntryCount / report.MatrixStatistics.TotalEntryCount * 100
                : 0,
            ChecklistCompletionRate = report.ChecklistStatistics.TotalItemCount > 0
                ? (double)report.ChecklistStatistics.CompletedItemCount / report.ChecklistStatistics.TotalItemCount * 100
                : 0
        };
    }
}

public class SystemStatisticsReport
{
    public DateTime GeneratedAt { get; set; }
    public string? ProjectId { get; set; }
    public ComponentStatistics ComponentStatistics { get; set; } = new();
    public EvidenceStatistics EvidenceStatistics { get; set; } = new();
    public ChecklistStatistics ChecklistStatistics { get; set; } = new();
    public MatrixStatistics MatrixStatistics { get; set; } = new();
    public OverallStatistics OverallStatistics { get; set; } = new();
}

public class ComponentStatistics
{
    public int TotalCount { get; set; }
    public int CategoryCount { get; set; }
    public Dictionary<string, int> CategoryBreakdown { get; set; } = new();
    public int ManufacturerCount { get; set; }
    public Dictionary<string, int> Manufacturers { get; set; } = new();
}

public class EvidenceStatistics
{
    public int TotalCount { get; set; }
    public int ValidCount { get; set; }
    public int ExpiredCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public Dictionary<string, int> TypeBreakdown { get; set; } = new();
}

public class ChecklistStatistics
{
    public int ChecklistCount { get; set; }
    public int TotalItemCount { get; set; }
    public int CompletedItemCount { get; set; }
    public int PendingItemCount { get; set; }
    public int FailedItemCount { get; set; }
    public Dictionary<string, int> StandardBreakdown { get; set; } = new();
}

public class MatrixStatistics
{
    public int MatrixCount { get; set; }
    public int TotalEntryCount { get; set; }
    public int CompliantEntryCount { get; set; }
    public int NonCompliantEntryCount { get; set; }
    public int PendingEntryCount { get; set; }
    public Dictionary<string, int> StandardBreakdown { get; set; } = new();
}

public class OverallStatistics
{
    public int TotalComponents { get; set; }
    public int TotalEvidence { get; set; }
    public int TotalChecklists { get; set; }
    public int TotalMatrices { get; set; }
    public double ComplianceRate { get; set; }
    public double ChecklistCompletionRate { get; set; }
}

