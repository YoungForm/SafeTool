namespace SafeTool.Application.Services;

/// <summary>
/// 双标准并行评估服务（P2优先级）
/// 同时评估ISO 13849-1和IEC 62061标准
/// </summary>
public class DualStandardEvaluationService
{
    private readonly ComplianceEvaluator _complianceEvaluator;
    private readonly IEC62061Evaluator _iec62061Evaluator;
    private readonly PlSilMappingService _plSilMappingService;

    public DualStandardEvaluationService(
        ComplianceEvaluator complianceEvaluator,
        IEC62061Evaluator iec62061Evaluator,
        PlSilMappingService plSilMappingService)
    {
        _complianceEvaluator = complianceEvaluator;
        _iec62061Evaluator = iec62061Evaluator;
        _plSilMappingService = plSilMappingService;
    }

    /// <summary>
    /// 执行双标准并行评估（增强版）
    /// </summary>
    public DualStandardEvaluationResult Evaluate(
        SafeTool.Domain.Compliance.ComplianceChecklist iso13849Checklist,
        SafeTool.Domain.Standards.SafetyFunction62061 iec62061Function,
        DualStandardEvaluationOptions? options = null)
    {
        var result = new DualStandardEvaluationResult
        {
            EvaluatedAt = DateTime.UtcNow
        };

        // 1. 执行ISO 13849-1评估
        var iso13849Result = _complianceEvaluator.Evaluate(iso13849Checklist);
        result.Iso13849Result = iso13849Result;

        // 2. 执行IEC 62061评估
        var (iec62061Result, _) = _iec62061Evaluator.Evaluate(iec62061Function);
        result.Iec62061Result = iec62061Result;

        // 3. 提取PL和SIL
        var achievedPL = ExtractPL(iso13849Result);
        var achievedSIL = ExtractSIL(iec62061Result);

        // 4. 执行PL↔SIL对照
        if (!string.IsNullOrEmpty(achievedPL) && !string.IsNullOrEmpty(achievedSIL))
        {
            var mapping = _plSilMappingService.Map(achievedPL, achievedSIL);
            result.PlSilMapping = mapping;
        }

        // 5. 一致性分析
        result.ConsistencyAnalysis = AnalyzeConsistency(iso13849Result, iec62061Result, result.PlSilMapping);

        // 6. 生成综合建议
        result.Recommendations = GenerateRecommendations(result);

        // 7. 生成详细对比报告（如果启用）
        if (options?.GenerateDetailedComparison == true)
        {
            result.DetailedComparison = GenerateDetailedComparison(iso13849Result, iec62061Result);
        }

        return result;
    }

    /// <summary>
    /// 生成详细对比报告
    /// </summary>
    private DetailedComparison GenerateDetailedComparison(
        SafeTool.Domain.Compliance.EvaluationResult iso13849Result,
        SafeTool.Domain.Standards.SafetyFunction62061 iec62061Function)
    {
        return new DetailedComparison
        {
            ComparisonItems = new List<ComparisonItem>
            {
                new ComparisonItem
                {
                    Aspect = "性能等级",
                    Iso13849Value = ExtractPL(iso13849Result),
                    Iec62061Value = "N/A",
                    Consistency = "N/A"
                },
                new ComparisonItem
                {
                    Aspect = "安全完整性等级",
                    Iso13849Value = "N/A",
                    Iec62061Value = iec62061Function.TargetSIL,
                    Consistency = "N/A"
                },
                new ComparisonItem
                {
                    Aspect = "符合性",
                    Iso13849Value = iso13849Result.OverallCompliance ? "符合" : "不符合",
                    Iec62061Value = iec62061Function.AchievedSIL == iec62061Function.TargetSIL ? "符合" : "不符合",
                    Consistency = (iso13849Result.OverallCompliance == (iec62061Function.AchievedSIL == iec62061Function.TargetSIL)) ? "一致" : "不一致"
                }
            }
        };
    }

    /// <summary>
    /// 提取PL
    /// </summary>
    private string ExtractPL(SafeTool.Domain.Compliance.EvaluationResult result)
    {
        // 从评估结果中提取PL
        if (result.OverallCompliance && result.Details != null)
        {
            foreach (var detail in result.Details)
            {
                if (detail.Contains("PL") && (detail.Contains("PLa") || detail.Contains("PLb") ||
                    detail.Contains("PLc") || detail.Contains("PLd") || detail.Contains("PLe")))
                {
                    if (detail.Contains("PLe")) return "PLe";
                    if (detail.Contains("PLd")) return "PLd";
                    if (detail.Contains("PLc")) return "PLc";
                    if (detail.Contains("PLb")) return "PLb";
                    if (detail.Contains("PLa")) return "PLa";
                }
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// 提取SIL
    /// </summary>
    private string ExtractSIL(SafeTool.Domain.Standards.SafetyFunction62061 function)
    {
        return function.TargetSIL;
    }

    /// <summary>
    /// 分析一致性
    /// </summary>
    private ConsistencyAnalysis AnalyzeConsistency(
        SafeTool.Domain.Compliance.EvaluationResult iso13849Result,
        SafeTool.Domain.Standards.SafetyFunction62061 iec62061Result,
        PlSilMappingResult? mapping)
    {
        var analysis = new ConsistencyAnalysis
        {
            IsConsistent = true,
            Issues = new List<string>()
        };

        if (mapping == null)
        {
            analysis.IsConsistent = false;
            analysis.Issues.Add("无法进行PL↔SIL对照（缺少PL或SIL信息）");
            return analysis;
        }

        if (!mapping.IsConsistent)
        {
            analysis.IsConsistent = false;
            analysis.Issues.AddRange(mapping.Warnings ?? new List<string>());
        }

        // 检查两个标准的符合性是否一致
        if (iso13849Result.OverallCompliance != (iec62061Result.AchievedSIL == iec62061Result.TargetSIL))
        {
            analysis.IsConsistent = false;
            analysis.Issues.Add("两个标准的符合性判断不一致");
        }

        return analysis;
    }

    /// <summary>
    /// 生成综合建议
    /// </summary>
    private List<string> GenerateRecommendations(DualStandardEvaluationResult result)
    {
        var recommendations = new List<string>();

        if (result.ConsistencyAnalysis != null && !result.ConsistencyAnalysis.IsConsistent)
        {
            recommendations.Add("⚠ 两个标准的评估结果存在不一致，建议重新审查设计");
            recommendations.AddRange(result.ConsistencyAnalysis.Issues.Select(i => $"  - {i}"));
        }

        if (result.PlSilMapping != null && result.PlSilMapping.IsConsistent)
        {
            recommendations.Add("✓ PL↔SIL对照一致");
            if (result.PlSilMapping.Notes != null && result.PlSilMapping.Notes.Any())
            {
                recommendations.AddRange(result.PlSilMapping.Notes.Select(n => $"  - {n}"));
            }
        }

        if (result.Iso13849Result.OverallCompliance && result.Iec62061Result.AchievedSIL == result.Iec62061Result.TargetSIL)
        {
            recommendations.Add("✓ 两个标准均满足要求");
        }

        return recommendations;
    }
}

public class DualStandardEvaluationResult
{
    public DateTime EvaluatedAt { get; set; }
    public SafeTool.Domain.Compliance.EvaluationResult? Iso13849Result { get; set; }
    public SafeTool.Domain.Standards.SafetyFunction62061? Iec62061Result { get; set; }
    public PlSilMappingResult? PlSilMapping { get; set; }
    public ConsistencyAnalysis? ConsistencyAnalysis { get; set; }
    public List<string> Recommendations { get; set; } = new();
    public DetailedComparison? DetailedComparison { get; set; }
}

public class ConsistencyAnalysis
{
    public bool IsConsistent { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class DualStandardEvaluationOptions
{
    public bool GenerateDetailedComparison { get; set; } = false;
    public bool IncludeRecommendations { get; set; } = true;
}

public class DetailedComparison
{
    public List<ComparisonItem> ComparisonItems { get; set; } = new();
}

public class ComparisonItem
{
    public string Aspect { get; set; } = string.Empty;
    public string Iso13849Value { get; set; } = string.Empty;
    public string Iec62061Value { get; set; } = string.Empty;
    public string Consistency { get; set; } = string.Empty;
}

public class DualStandardEvaluationOptions
{
    public bool GenerateDetailedComparison { get; set; } = false;
    public bool IncludeRecommendations { get; set; } = true;
}

public class DetailedComparison
{
    public List<ComparisonItem> ComparisonItems { get; set; } = new();
}

public class ComparisonItem
{
    public string Aspect { get; set; } = string.Empty;
    public string Iso13849Value { get; set; } = string.Empty;
    public string Iec62061Value { get; set; } = string.Empty;
    public string Consistency { get; set; } = string.Empty;
}

