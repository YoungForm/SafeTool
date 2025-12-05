namespace SafeTool.Application.Services;

/// <summary>
/// IEC 62061 计算增强服务
/// </summary>
public class Iec62061CalculationEnhancementService
{
    /// <summary>
    /// 检查超期风险
    /// </summary>
    public ExpiryRiskCheckResult CheckExpiryRisk(
        double proofTestIntervalT1,
        double missionTimeT10D,
        DateTime? lastTestDate = null)
    {
        var result = new ExpiryRiskCheckResult
        {
            T1 = proofTestIntervalT1,
            T10D = missionTimeT10D,
            Warnings = new List<string>(),
            Recommendations = new List<string>()
        };

        // 检查T1和T10D的关系
        if (proofTestIntervalT1 > missionTimeT10D)
        {
            result.RiskLevel = ExpiryRiskLevel.Critical;
            result.Warnings.Add($"⚠️ 严重：T1 ({proofTestIntervalT1}小时) 大于T10D ({missionTimeT10D}小时)");
            result.Warnings.Add("证明试验间隔超过有用寿命，PFHd计算可能失真");
            result.Recommendations.Add("立即缩短证明试验间隔或延长有用寿命");
            result.Recommendations.Add("重新评估PFHd计算的有效性");
        }
        else if (proofTestIntervalT1 > missionTimeT10D * 0.8)
        {
            result.RiskLevel = ExpiryRiskLevel.High;
            result.Warnings.Add($"⚠️ 警告：T1 ({proofTestIntervalT1}小时) 接近T10D ({missionTimeT10D}小时)");
            result.Warnings.Add("建议缩短证明试验间隔以确保安全裕量");
            result.Recommendations.Add($"建议T1不超过T10D的50%，即不超过{missionTimeT10D * 0.5:F0}小时");
        }
        else if (proofTestIntervalT1 > missionTimeT10D * 0.5)
        {
            result.RiskLevel = ExpiryRiskLevel.Medium;
            result.Warnings.Add($"注意：T1 ({proofTestIntervalT1}小时) 占T10D的{(proofTestIntervalT1 / missionTimeT10D * 100):F0}%");
            result.Recommendations.Add("建议进一步缩短证明试验间隔以提高安全性");
        }
        else
        {
            result.RiskLevel = ExpiryRiskLevel.Low;
        }

        // 检查使用寿命超期风险
        var typicalLifetime = 87600; // 10年，约87600小时
        if (missionTimeT10D > typicalLifetime * 1.5)
        {
            result.Warnings.Add($"⚠️ 警告：T10D ({missionTimeT10D}小时，约{missionTimeT10D / 8760:F1}年) 超过典型使用寿命");
            result.Recommendations.Add("需确认设备实际寿命与维护策略");
            result.Recommendations.Add("考虑设备更换计划");
        }

        // 检查下次测试时间
        if (lastTestDate.HasValue)
        {
            var nextTestDate = lastTestDate.Value.AddHours(proofTestIntervalT1);
            var daysUntilTest = (nextTestDate - DateTime.UtcNow).TotalDays;

            if (daysUntilTest < 0)
            {
                result.RiskLevel = ExpiryRiskLevel.Critical;
                result.Warnings.Add($"⚠️ 严重：证明试验已逾期 {Math.Abs(daysUntilTest):F0} 天");
                result.Recommendations.Add("立即执行证明试验");
            }
            else if (daysUntilTest < 30)
            {
                result.Warnings.Add($"⚠️ 警告：证明试验将在 {daysUntilTest:F0} 天后到期");
                result.Recommendations.Add("安排证明试验计划");
            }
        }

        return result;
    }

    /// <summary>
    /// 校核证明试验覆盖率
    /// </summary>
    public ProofTestCoverageResult CheckProofTestCoverage(
        double proofTestIntervalT1,
        double missionTimeT10D,
        double? coverage = null)
    {
        var result = new ProofTestCoverageResult
        {
            T1 = proofTestIntervalT1,
            T10D = missionTimeT10D,
            Warnings = new List<string>(),
            Recommendations = new List<string>()
        };

        // 计算覆盖率比例
        var coverageRatio = proofTestIntervalT1 / missionTimeT10D;
        result.CoverageRatio = coverageRatio;

        // 评估覆盖率
        if (coverageRatio > 0.5)
        {
            result.IsAdequate = false;
            result.Warnings.Add($"⚠️ 证明试验间隔占比 {coverageRatio:P0}，超过50%上限");
            result.Warnings.Add("证明试验覆盖率不足，可能无法有效检测所有故障");
            result.Recommendations.Add($"建议缩短T1至不超过T10D的50%，即不超过{missionTimeT10D * 0.5:F0}小时");
        }
        else if (coverageRatio > 0.3)
        {
            result.IsAdequate = true;
            result.Warnings.Add($"注意：证明试验间隔占比 {coverageRatio:P0}，建议进一步优化");
            result.Recommendations.Add("考虑缩短证明试验间隔以提高覆盖率");
        }
        else
        {
            result.IsAdequate = true;
        }

        // 如果提供了具体覆盖率值
        if (coverage.HasValue)
        {
            result.Coverage = coverage.Value;
            if (coverage.Value < 0.9)
            {
                result.Warnings.Add($"⚠️ 证明试验覆盖率 {coverage.Value:P0} 低于推荐值90%");
                result.Recommendations.Add("提高证明试验覆盖率至90%以上");
            }
        }

        return result;
    }
}

public class ExpiryRiskCheckResult
{
    public double T1 { get; set; }
    public double T10D { get; set; }
    public ExpiryRiskLevel RiskLevel { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public enum ExpiryRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public class ProofTestCoverageResult
{
    public double T1 { get; set; }
    public double T10D { get; set; }
    public double CoverageRatio { get; set; }
    public double? Coverage { get; set; }
    public bool IsAdequate { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

