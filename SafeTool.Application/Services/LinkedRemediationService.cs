namespace SafeTool.Application.Services;

/// <summary>
/// 联动整改建议服务（P2优先级）
/// 基于PL↔SIL映射生成联动整改建议
/// </summary>
public class LinkedRemediationService
{
    private readonly PlSilMappingService _plSilMappingService;
    private readonly RemediationTrackingService _remediationTrackingService;

    public LinkedRemediationService(
        PlSilMappingService plSilMappingService,
        RemediationTrackingService remediationTrackingService)
    {
        _plSilMappingService = plSilMappingService;
        _remediationTrackingService = remediationTrackingService;
    }

    /// <summary>
    /// 生成联动整改建议
    /// </summary>
    public LinkedRemediationResult GenerateLinkedRemediations(
        string projectId,
        string? currentPL,
        string? currentSIL,
        string? targetPL,
        string? targetSIL)
    {
        var result = new LinkedRemediationResult
        {
            ProjectId = projectId,
            GeneratedAt = DateTime.UtcNow
        };

        // 1. 执行PL↔SIL对照
        if (!string.IsNullOrEmpty(currentPL) && !string.IsNullOrEmpty(currentSIL))
        {
            var mapping = _plSilMappingService.Map(currentPL, currentSIL);
            result.CurrentMapping = mapping;

            if (!mapping.IsConsistent)
            {
                result.HasInconsistency = true;
                result.InconsistencyIssues = mapping.Warnings ?? new List<string>();
            }
        }

        // 2. 生成目标对照
        if (!string.IsNullOrEmpty(targetPL) && !string.IsNullOrEmpty(targetSIL))
        {
            var targetMapping = _plSilMappingService.Map(targetPL, targetSIL);
            result.TargetMapping = targetMapping;
        }

        // 3. 生成联动整改建议
        result.Remediations = GenerateRemediationItems(result);

        // 4. 自动创建整改项
        if (result.Remediations.Any())
        {
            foreach (var remediation in result.Remediations)
            {
                try
                {
                    var remediationItem = _remediationTrackingService.CreateRemediationItem(
                        projectId,
                        remediation.Title,
                        remediation.Description,
                        remediation.Standard,
                        remediation.Clause,
                        remediation.Priority,
                        remediation.DueDate);

                    remediation.RemediationItemId = remediationItem.Id;
                }
                catch (Exception ex)
                {
                    remediation.Error = ex.Message;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 生成整改项
    /// </summary>
    private List<LinkedRemediationItem> GenerateRemediationItems(LinkedRemediationResult result)
    {
        var items = new List<LinkedRemediationItem>();

        // 如果存在不一致，生成整改建议
        if (result.HasInconsistency && result.CurrentMapping != null)
        {
            items.Add(new LinkedRemediationItem
            {
                Title = "PL↔SIL对照不一致",
                Description = string.Join("\n", result.InconsistencyIssues ?? new List<string>()),
                Standard = "ISO 13849-1 / IEC 62061",
                Clause = "交叉映射",
                Priority = "High",
                DueDate = DateTime.UtcNow.AddDays(30),
                LinkedStandards = new List<string> { "ISO 13849-1", "IEC 62061" }
            });
        }

        // 如果目标映射与当前不一致，生成提升建议
        if (result.TargetMapping != null && result.CurrentMapping != null)
        {
            var currentPL = ExtractPLFromMapping(result.CurrentMapping);
            var targetPL = ExtractPLFromMapping(result.TargetMapping);

            if (ComparePL(currentPL, targetPL) < 0)
            {
                items.Add(new LinkedRemediationItem
                {
                    Title = "提升性能等级以满足目标要求",
                    Description = $"当前PL: {currentPL}, 目标PL: {targetPL}。需要提升系统架构或参数以满足目标要求。",
                    Standard = "ISO 13849-1",
                    Clause = "性能等级评估",
                    Priority = "High",
                    DueDate = DateTime.UtcNow.AddDays(60),
                    LinkedStandards = new List<string> { "ISO 13849-1" }
                });
            }
        }

        return items;
    }

    /// <summary>
    /// 从映射结果中提取PL
    /// </summary>
    private string ExtractPLFromMapping(PlSilMappingResult mapping)
    {
        // 从映射结果中提取PL
        if (!string.IsNullOrEmpty(mapping.AchievedPL))
            return mapping.AchievedPL;

        return string.Empty;
    }

    /// <summary>
    /// 比较PL等级
    /// </summary>
    private int ComparePL(string pl1, string pl2)
    {
        var plOrder = new Dictionary<string, int>
        {
            { "PLa", 1 },
            { "PLb", 2 },
            { "PLc", 3 },
            { "PLd", 4 },
            { "PLe", 5 }
        };

        var order1 = plOrder.GetValueOrDefault(pl1, 0);
        var order2 = plOrder.GetValueOrDefault(pl2, 0);

        return order1.CompareTo(order2);
    }
}

public class LinkedRemediationResult
{
    public string ProjectId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public PlSilMappingResult? CurrentMapping { get; set; }
    public PlSilMappingResult? TargetMapping { get; set; }
    public bool HasInconsistency { get; set; }
    public List<string>? InconsistencyIssues { get; set; }
    public List<LinkedRemediationItem> Remediations { get; set; } = new();
}

public class LinkedRemediationItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Standard { get; set; } = string.Empty;
    public string Clause { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public DateTime? DueDate { get; set; }
    public List<string> LinkedStandards { get; set; } = new();
    public string? RemediationItemId { get; set; }
    public string? Error { get; set; }
}

