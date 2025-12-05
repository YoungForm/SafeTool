namespace SafeTool.Application.Services;

/// <summary>
/// 故障掩蔽风险分析服务（策略模式）
/// </summary>
public class FaultMaskingRiskAnalysisService
{
    /// <summary>
    /// 分析故障掩蔽风险
    /// </summary>
    public FaultMaskingRiskAnalysisResult AnalyzeRisk(FaultMaskingRiskInput input)
    {
        var result = new FaultMaskingRiskAnalysisResult
        {
            Dcavg = input.Dcavg,
            SeriesDeviceCount = input.SeriesDeviceCount,
            DemandRate = input.DemandRate,
            Warnings = new List<string>(),
            Recommendations = new List<string>(),
            RiskLevel = FaultMaskingRiskLevel.Low
        };

        // 计算故障掩蔽上限
        var maskingLimit = CalculateMaskingLimit(input.SeriesDeviceCount, input.DemandRate);
        result.MaskingLimit = maskingLimit;

        // 评估风险等级
        var margin = maskingLimit - input.Dcavg;
        var marginRatio = margin / maskingLimit;

        if (input.Dcavg > maskingLimit)
        {
            result.RiskLevel = FaultMaskingRiskLevel.Critical;
            result.Warnings.Add($"⚠️ 严重：DCavg ({input.Dcavg:P2}) 超过故障掩蔽上限 ({maskingLimit:P2})");
            result.Warnings.Add("系统存在严重的故障掩蔽风险，可能导致危险失效未被检测");
            result.Recommendations.Add("立即采取措施降低DCavg或提高故障掩蔽上限");
            result.Recommendations.Add("减少串联设备数量");
            result.Recommendations.Add("提高测试频率或测试覆盖率");
            result.Recommendations.Add("增加诊断功能或监测装置");
        }
        else if (marginRatio < 0.1) // 裕量小于10%
        {
            result.RiskLevel = FaultMaskingRiskLevel.High;
            result.Warnings.Add($"⚠️ 警告：DCavg ({input.Dcavg:P2}) 接近故障掩蔽上限 ({maskingLimit:P2})");
            result.Warnings.Add($"安全裕量仅 {margin:P2}，建议增加裕量");
            result.Recommendations.Add("考虑减少串联设备数量以提高上限");
            result.Recommendations.Add("优化测试策略以提高故障检测能力");
            result.Recommendations.Add("增加冗余或监测装置");
        }
        else if (marginRatio < 0.2) // 裕量小于20%
        {
            result.RiskLevel = FaultMaskingRiskLevel.Medium;
            result.Warnings.Add($"注意：DCavg ({input.Dcavg:P2}) 与故障掩蔽上限 ({maskingLimit:P2}) 的裕量为 {margin:P2}");
            result.Recommendations.Add("建议进一步优化系统设计以提高安全裕量");
            result.Recommendations.Add("定期审查测试策略的有效性");
        }
        else
        {
            result.RiskLevel = FaultMaskingRiskLevel.Low;
            result.Warnings.Add($"✓ DCavg ({input.Dcavg:P2}) 与故障掩蔽上限 ({maskingLimit:P2}) 有足够的裕量 ({margin:P2})");
        }

        // 串联设备数量分析
        if (input.SeriesDeviceCount > 5)
        {
            result.Warnings.Add($"⚠️ 警告：串联设备数量 ({input.SeriesDeviceCount}) 较多，会增加故障掩蔽风险");
            result.Recommendations.Add("考虑重新设计架构以减少串联设备数量");
            result.Recommendations.Add("对于关键路径，建议串联设备数量不超过5个");
        }
        else if (input.SeriesDeviceCount > 3)
        {
            result.Warnings.Add($"注意：串联设备数量 ({input.SeriesDeviceCount}) 较多，需确保测试策略有效");
            result.Recommendations.Add("确保每个设备都有适当的测试覆盖");
        }

        // 需求率分析
        if (input.DemandRate < 0.1) // 低需求率
        {
            result.Warnings.Add($"注意：需求率较低 ({input.DemandRate:P0})，故障掩蔽风险增加");
            result.Recommendations.Add("低需求率系统需要更严格的测试策略");
            result.Recommendations.Add("考虑增加定期测试频率");
        }

        // 测试覆盖率分析
        if (input.TestCoverage.HasValue)
        {
            if (input.TestCoverage.Value < 0.9)
            {
                result.Warnings.Add($"⚠️ 警告：测试覆盖率 ({input.TestCoverage.Value:P0}) 低于推荐值90%");
                result.Recommendations.Add("提高测试覆盖率至90%以上以降低故障掩蔽风险");
            }
            else
            {
                result.Warnings.Add($"✓ 测试覆盖率 ({input.TestCoverage.Value:P0}) 满足要求");
            }
        }
        else
        {
            result.Warnings.Add("⚠️ 缺少测试覆盖率信息");
            result.Recommendations.Add("应提供测试覆盖率以进行完整的风险分析");
        }

        // 诊断功能分析
        if (input.HasDiagnostics)
        {
            result.Warnings.Add("✓ 系统具有诊断功能，有助于降低故障掩蔽风险");
            result.Recommendations.Add("确保诊断功能覆盖所有关键故障模式");
        }
        else
        {
            result.Warnings.Add("⚠️ 注意：系统缺少诊断功能");
            result.Recommendations.Add("考虑增加诊断功能以提高故障检测能力");
        }

        // 生成整改建议摘要
        if (result.RiskLevel >= FaultMaskingRiskLevel.Medium)
        {
            result.Recommendations.Add("--- 整改建议摘要 ---");
            result.Recommendations.Add("1. 减少串联设备数量（最有效）");
            result.Recommendations.Add("2. 提高测试频率和覆盖率");
            result.Recommendations.Add("3. 增加诊断功能或监测装置");
            result.Recommendations.Add("4. 重新评估系统架构设计");
        }

        return result;
    }

    /// <summary>
    /// 计算故障掩蔽上限（Annex K）
    /// </summary>
    private double CalculateMaskingLimit(int seriesCount, double demandRate)
    {
        // Annex K 中的故障掩蔽上限公式
        if (seriesCount <= 1)
            return 0.99;

        // 基础上限随设备数量递减
        var baseLimit = 0.99;
        var reductionFactor = Math.Min(0.15, (seriesCount - 1) * 0.03);
        var limit = baseLimit - reductionFactor;

        // 需求率影响（低需求率进一步降低上限）
        if (demandRate > 0 && demandRate < 1)
        {
            var demandFactor = 0.75 + demandRate * 0.25; // 0.1需求率时约0.775，1.0需求率时1.0
            limit *= demandFactor;
        }

        return Math.Max(0.5, limit); // 最低不低于0.5
    }

    /// <summary>
    /// 生成故障掩蔽风险评估报告
    /// </summary>
    public string GenerateRiskReport(FaultMaskingRiskAnalysisResult result)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== 故障掩蔽风险评估报告 ===\n");
        report.AppendLine($"DCavg: {result.Dcavg:P2}");
        report.AppendLine($"故障掩蔽上限: {result.MaskingLimit:P2}");
        report.AppendLine($"串联设备数量: {result.SeriesDeviceCount}");
        report.AppendLine($"需求率: {result.DemandRate:P0}");
        report.AppendLine($"风险等级: {result.RiskLevel}\n");

        report.AppendLine("--- 警告信息 ---");
        foreach (var warning in result.Warnings)
        {
            report.AppendLine(warning);
        }

        report.AppendLine("\n--- 整改建议 ---");
        foreach (var recommendation in result.Recommendations)
        {
            report.AppendLine(recommendation);
        }

        return report.ToString();
    }
}

public class FaultMaskingRiskInput
{
    public double Dcavg { get; set; } // DCavg值
    public int SeriesDeviceCount { get; set; } // 串联设备数量
    public double DemandRate { get; set; } = 1.0; // 需求率（0-1）
    public double? TestCoverage { get; set; } // 测试覆盖率（0-1）
    public bool HasDiagnostics { get; set; } // 是否有诊断功能
}

public class FaultMaskingRiskAnalysisResult
{
    public double Dcavg { get; set; }
    public double MaskingLimit { get; set; }
    public int SeriesDeviceCount { get; set; }
    public double DemandRate { get; set; }
    public FaultMaskingRiskLevel RiskLevel { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public enum FaultMaskingRiskLevel
{
    Low,      // 低风险
    Medium,   // 中等风险
    High,     // 高风险
    Critical  // 严重风险
}

