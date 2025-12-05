namespace SafeTool.Application.Services;

/// <summary>
/// SRS任务单联动服务（观察者模式）
/// </summary>
public class SrsTaskLinkageService
{
    private readonly SrsService _srsService;
    private readonly VerificationChecklistService _verificationService;
    private readonly Iso13849VerificationEnhancementService _verificationEnhancement;

    public SrsTaskLinkageService(
        SrsService srsService,
        VerificationChecklistService verificationService,
        Iso13849VerificationEnhancementService verificationEnhancement)
    {
        _srsService = srsService;
        _verificationService = verificationService;
        _verificationEnhancement = verificationEnhancement;
    }

    /// <summary>
    /// 从SRS自动生成验证任务
    /// </summary>
    public IEnumerable<VerificationTask> GenerateVerificationTasksFromSrs(string srsId, string projectId)
    {
        var srs = _srsService.Get(srsId);
        if (srs == null)
            throw new KeyNotFoundException($"SRS {srsId} 不存在");

        var tasks = new List<VerificationTask>();

        // 根据SRS的关键参数生成验证任务
        if (!string.IsNullOrWhiteSpace(srs.ArchitectureCategory))
        {
            tasks.Add(new VerificationTask
            {
                Code = "VER-CAT",
                Title = "类别验证",
                Description = $"验证架构类别 {srs.ArchitectureCategory} 的选择与实现",
                Standard = "ISO13849-2",
                Clause = "6",
                RelatedSrsId = srsId,
                RelatedRequirement = "架构类别",
                Priority = "High"
            });
        }

        if (srs.DCavg > 0)
        {
            tasks.Add(new VerificationTask
            {
                Code = "VER-DCAVG",
                Title = "DCavg计算验证",
                Description = $"验证DCavg {srs.DCavg:P2} 的计算与故障掩蔽评估",
                Standard = "ISO13849-2",
                Clause = "Annex K",
                RelatedSrsId = srsId,
                RelatedRequirement = "DCavg",
                Priority = "High"
            });
        }

        if (srs.MTTFd > 0)
        {
            tasks.Add(new VerificationTask
            {
                Code = "VER-MTTFD",
                Title = "MTTFd验证",
                Description = $"验证MTTFd {srs.MTTFd:0} 小时的选择与证据",
                Standard = "ISO13849-2",
                Clause = "5.2",
                RelatedSrsId = srsId,
                RelatedRequirement = "MTTFd",
                Priority = "Medium"
            });
        }

        if (!string.IsNullOrWhiteSpace(srs.CCFMeasures))
        {
            tasks.Add(new VerificationTask
            {
                Code = "VER-CCF",
                Title = "CCF措施验证",
                Description = $"验证CCF措施的实施与评分≥65",
                Standard = "ISO13849-2",
                Clause = "Annex F",
                RelatedSrsId = srsId,
                RelatedRequirement = "CCF措施",
                Priority = "High"
            });
        }

        // 根据SRS需求生成验证任务
        foreach (var req in srs.Requirements)
        {
            if (req.Mandatory)
            {
                tasks.Add(new VerificationTask
                {
                    Code = $"VER-REQ-{req.ClauseRef}",
                    Title = $"需求验证：{req.Title}",
                    Description = req.Description,
                    Standard = "ISO13849-2",
                    Clause = req.ClauseRef,
                    RelatedSrsId = srsId,
                    RelatedRequirement = req.Title,
                    Priority = "High"
                });
            }
        }

        // 保存到验证清单
        foreach (var task in tasks)
        {
            _verificationService.Upsert(projectId, task.Standard, new VerificationChecklistService.Item
            {
                Code = task.Code,
                Title = task.Title,
                Clause = task.Clause,
                Description = task.Description
            });
        }

        return tasks;
    }

    /// <summary>
    /// 检查SRS与验证任务的追溯关系
    /// </summary>
    public SrsTraceabilityResult CheckSrsTraceability(string srsId, string projectId)
    {
        var srs = _srsService.Get(srsId);
        if (srs == null)
            throw new KeyNotFoundException($"SRS {srsId} 不存在");

        var result = new SrsTraceabilityResult
        {
            SrsId = srsId,
            ProjectId = projectId,
            Issues = new List<TraceabilityIssue>()
        };

        // 获取所有验证任务
        var iso13849Tasks = _verificationService.Get(projectId, "ISO13849-2").ToList();
        var iec60204Tasks = _verificationService.Get(projectId, "IEC60204-1").ToList();

        // 检查关键参数是否有对应的验证任务
        if (!string.IsNullOrWhiteSpace(srs.ArchitectureCategory))
        {
            var hasCategoryTask = iso13849Tasks.Any(t => t.Code == "VER-CAT" || t.Title.Contains("类别"));
            if (!hasCategoryTask)
            {
                result.Issues.Add(new TraceabilityIssue
                {
                    Type = "MissingVerification",
                    Severity = "High",
                    Message = $"SRS中定义了架构类别 {srs.ArchitectureCategory}，但缺少对应的验证任务"
                });
            }
        }

        if (srs.DCavg > 0)
        {
            var hasDcavgTask = iso13849Tasks.Any(t => t.Code == "VER-DCAVG" || t.Title.Contains("DCavg"));
            if (!hasDcavgTask)
            {
                result.Issues.Add(new TraceabilityIssue
                {
                    Type = "MissingVerification",
                    Severity = "High",
                    Message = $"SRS中定义了DCavg {srs.DCavg:P2}，但缺少对应的验证任务"
                });
            }
        }

        // 检查SRS需求是否有对应的验证任务
        foreach (var req in srs.Requirements.Where(r => r.Mandatory))
        {
            var hasTask = iso13849Tasks.Any(t => t.Description.Contains(req.Title) || t.Clause == req.ClauseRef);
            if (!hasTask)
            {
                result.Issues.Add(new TraceabilityIssue
                {
                    Type = "MissingVerification",
                    Severity = "Medium",
                    Message = $"SRS需求 {req.Title} 缺少对应的验证任务"
                });
            }
        }

        result.IsComplete = result.Issues.Count == 0;
        return result;
    }
}

public class VerificationTask
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Standard { get; set; } = string.Empty;
    public string Clause { get; set; } = string.Empty;
    public string? RelatedSrsId { get; set; }
    public string? RelatedRequirement { get; set; }
    public string Priority { get; set; } = "Medium"; // Low/Medium/High
}

public class SrsTraceabilityResult
{
    public string SrsId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public List<TraceabilityIssue> Issues { get; set; } = new();
}

public class TraceabilityIssue
{
    public string Type { get; set; } = string.Empty; // MissingVerification/IncompleteVerification
    public string Severity { get; set; } = string.Empty; // Low/Medium/High
    public string Message { get; set; } = string.Empty;
}

