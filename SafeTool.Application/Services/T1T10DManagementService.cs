namespace SafeTool.Application.Services;

/// <summary>
/// T1/T10D参数管理服务（增强版）
/// </summary>
public class T1T10DManagementService
{
    private readonly Iec62061CalculationEnhancementService _calculationService;

    public T1T10DManagementService(Iec62061CalculationEnhancementService calculationService)
    {
        _calculationService = calculationService;
    }

    /// <summary>
    /// 综合T1/T10D参数管理与风险评估
    /// </summary>
    public T1T10DManagementResult ManageParameters(T1T10DParameters parameters)
    {
        var result = new T1T10DManagementResult
        {
            Parameters = parameters,
            Warnings = new List<string>(),
            Recommendations = new List<string>(),
            RiskLevel = ExpiryRiskLevel.Low,
            NextActions = new List<NextAction>()
        };

        // 执行超期风险检查
        var expiryRisk = _calculationService.CheckExpiryRisk(
            parameters.ProofTestIntervalT1,
            parameters.MissionTimeT10D,
            parameters.LastTestDate);

        result.RiskLevel = expiryRisk.RiskLevel;
        result.Warnings.AddRange(expiryRisk.Warnings);
        result.Recommendations.AddRange(expiryRisk.Recommendations);

        // 执行证明试验覆盖率检查
        var coverageCheck = _calculationService.CheckProofTestCoverage(
            parameters.ProofTestIntervalT1,
            parameters.MissionTimeT10D,
            parameters.ProofTestCoverage);

        result.CoverageRatio = coverageCheck.CoverageRatio;
        result.IsCoverageAdequate = coverageCheck.IsAdequate;
        result.Warnings.AddRange(coverageCheck.Warnings);
        result.Recommendations.AddRange(coverageCheck.Recommendations);

        // 计算下次测试时间
        if (parameters.LastTestDate.HasValue)
        {
            var nextTestDate = parameters.LastTestDate.Value.AddHours(parameters.ProofTestIntervalT1);
            result.NextTestDate = nextTestDate;
            var daysUntilTest = (nextTestDate - DateTime.UtcNow).TotalDays;

            if (daysUntilTest < 0)
            {
                result.NextActions.Add(new NextAction
                {
                    Action = "立即执行证明试验",
                    Priority = "Critical",
                    DueDate = DateTime.UtcNow,
                    Description = $"证明试验已逾期 {Math.Abs(daysUntilTest):F0} 天"
                });
            }
            else if (daysUntilTest < 30)
            {
                result.NextActions.Add(new NextAction
                {
                    Action = "安排证明试验计划",
                    Priority = "High",
                    DueDate = nextTestDate,
                    Description = $"证明试验将在 {daysUntilTest:F0} 天后到期"
                });
            }
            else if (daysUntilTest < 90)
            {
                result.NextActions.Add(new NextAction
                {
                    Action = "准备证明试验",
                    Priority = "Medium",
                    DueDate = nextTestDate,
                    Description = $"证明试验将在 {daysUntilTest:F0} 天后到期"
                });
            }
        }
        else
        {
            result.Warnings.Add("⚠️ 缺少上次测试日期信息");
            result.Recommendations.Add("应记录上次证明试验日期以进行到期提醒");
        }

        // 参数优化建议
        var optimalT1 = parameters.MissionTimeT10D * 0.5;
        if (parameters.ProofTestIntervalT1 > optimalT1)
        {
            result.Recommendations.Add($"建议优化T1至不超过T10D的50%，即不超过{optimalT1:F0}小时");
            result.OptimalT1 = optimalT1;
        }

        // 使用寿命评估
        var typicalLifetime = 87600; // 10年
        var lifetimeRatio = parameters.MissionTimeT10D / typicalLifetime;
        if (lifetimeRatio > 1.5)
        {
            result.Warnings.Add($"⚠️ T10D ({parameters.MissionTimeT10D / 8760:F1}年) 显著超过典型使用寿命（10年）");
            result.Recommendations.Add("需确认设备实际寿命与维护策略");
            result.Recommendations.Add("考虑设备更换计划或重新评估T10D值");
        }
        else if (lifetimeRatio > 1.0)
        {
            result.Warnings.Add($"注意：T10D ({parameters.MissionTimeT10D / 8760:F1}年) 超过典型使用寿命");
            result.Recommendations.Add("建议定期评估设备实际寿命");
        }

        // 生成参数摘要
        result.Summary = GenerateSummary(result);

        return result;
    }

    /// <summary>
    /// 建议T1/T10D参数
    /// </summary>
    public T1T10DRecommendation SuggestParameters(double targetSIL, double? currentT10D = null)
    {
        var recommendation = new T1T10DRecommendation
        {
            TargetSIL = targetSIL,
            Recommendations = new List<string>()
        };

        // 基于SIL等级建议T10D
        var suggestedT10D = currentT10D ?? (targetSIL switch
        {
            1 => 87600,  // 10年
            2 => 87600,  // 10年
            3 => 43800,  // 5年（更保守）
            _ => 87600
        });

        recommendation.SuggestedT10D = suggestedT10D;
        recommendation.SuggestedT1 = suggestedT10D * 0.5; // T1应为T10D的50%或更少

        recommendation.Recommendations.Add($"建议T10D：{suggestedT10D:F0}小时（约{suggestedT10D / 8760:F1}年）");
        recommendation.Recommendations.Add($"建议T1：{recommendation.SuggestedT1:F0}小时（约{recommendation.SuggestedT1 / 8760:F2}年）");
        recommendation.Recommendations.Add("T1应不超过T10D的50%以确保足够的测试覆盖率");

        return recommendation;
    }

    private string GenerateSummary(T1T10DManagementResult result)
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine($"T1: {result.Parameters.ProofTestIntervalT1:F0}小时（约{result.Parameters.ProofTestIntervalT1 / 8760:F2}年）");
        summary.AppendLine($"T10D: {result.Parameters.MissionTimeT10D:F0}小时（约{result.Parameters.MissionTimeT10D / 8760:F1}年）");
        summary.AppendLine($"风险等级: {result.RiskLevel}");
        summary.AppendLine($"覆盖率比例: {result.CoverageRatio:P0}");
        
        if (result.NextTestDate.HasValue)
        {
            var daysUntilTest = (result.NextTestDate.Value - DateTime.UtcNow).TotalDays;
            summary.AppendLine($"下次测试: {result.NextTestDate.Value:yyyy-MM-dd}（{daysUntilTest:F0}天后）");
        }

        return summary.ToString();
    }
}

public class T1T10DParameters
{
    public double ProofTestIntervalT1 { get; set; } // 证明试验间隔（小时）
    public double MissionTimeT10D { get; set; } // 使命时间（小时）
    public DateTime? LastTestDate { get; set; } // 上次测试日期
    public double? ProofTestCoverage { get; set; } // 证明试验覆盖率（0-1）
}

public class T1T10DManagementResult
{
    public T1T10DParameters Parameters { get; set; } = new();
    public ExpiryRiskLevel RiskLevel { get; set; }
    public double CoverageRatio { get; set; }
    public bool IsCoverageAdequate { get; set; }
    public DateTime? NextTestDate { get; set; }
    public double? OptimalT1 { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<NextAction> NextActions { get; set; } = new();
    public string? Summary { get; set; }
}

public class NextAction
{
    public string Action { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty; // Critical/High/Medium/Low
    public DateTime DueDate { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class T1T10DRecommendation
{
    public double TargetSIL { get; set; }
    public double SuggestedT10D { get; set; }
    public double SuggestedT1 { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

