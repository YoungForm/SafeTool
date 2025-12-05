namespace SafeTool.Application.Services;

/// <summary>
/// ISO 13849-1 计算增强服务（策略模式）
/// </summary>
public class Iso13849CalculationEnhancementService
{
    /// <summary>
    /// 计算串联设备DCavg（常规法 - Annex K）
    /// </summary>
    public DcavgCalculationResult CalculateDcavgRegular(
        List<DeviceDcavgInfo> devices,
        double demandRate,
        int seriesCount)
    {
        if (devices.Count == 0)
            return new DcavgCalculationResult { Dcavg = 0, Method = "regular", Warnings = new List<string> { "未提供设备信息" } };

        // Annex K 常规法计算
        // DCavg = 1 - (1 - DC1) * (1 - DC2) * ... * (1 - DCn) * (1 - DCtest)
        double product = 1.0;
        var warnings = new List<string>();

        foreach (var device in devices)
        {
            if (device.Dcavg < 0 || device.Dcavg > 1)
            {
                warnings.Add($"设备 {device.Id} 的DCavg值 {device.Dcavg} 超出有效范围 [0,1]");
                continue;
            }
            product *= (1 - device.Dcavg);
        }

        // 考虑测试设备的影响
        var testDc = 0.0;
        if (demandRate > 0)
        {
            // 测试覆盖率取决于需求率和测试频率
            testDc = Math.Min(0.99, demandRate * 0.1); // 简化计算
        }

        var dcavg = 1 - product * (1 - testDc);

        // 故障掩蔽上限检查
        var maskingLimit = CalculateMaskingLimit(seriesCount, demandRate);
        if (dcavg > maskingLimit)
        {
            warnings.Add($"⚠️ 故障掩蔽风险：DCavg ({dcavg:P2}) 超过上限 ({maskingLimit:P2})");
            warnings.Add($"建议：减少串联设备数量或提高测试频率");
            dcavg = Math.Min(dcavg, maskingLimit);
        }

        return new DcavgCalculationResult
        {
            Dcavg = dcavg,
            Method = "regular",
            MaskingLimit = maskingLimit,
            Warnings = warnings
        };
    }

    /// <summary>
    /// 计算故障掩蔽上限（Annex K）
    /// </summary>
    private double CalculateMaskingLimit(int seriesCount, double demandRate)
    {
        // Annex K 中的故障掩蔽上限公式
        // 上限取决于串联设备数量和需求率
        if (seriesCount <= 1)
            return 0.99;

        // 简化公式：上限随设备数量递减
        var baseLimit = 0.99;
        var reductionFactor = Math.Min(0.1, (seriesCount - 1) * 0.02);
        var limit = baseLimit - reductionFactor;

        // 需求率影响
        if (demandRate > 0 && demandRate < 1)
        {
            limit *= (0.8 + demandRate * 0.2); // 低需求率进一步降低上限
        }

        return Math.Max(0.5, limit); // 最低不低于0.5
    }

    /// <summary>
    /// 类别选型助手（增强版）
    /// </summary>
    public CategorySelectionResult SuggestCategory(CategorySelectionInput input)
    {
        var result = new CategorySelectionResult
        {
            Suggestions = new List<CategorySuggestion>(),
            Conflicts = new List<string>(),
            Recommendations = new List<string>()
        };

        // 分析输入特征
        var hasRedundancy = input.InputRedundancy || input.LogicRedundancy || input.OutputRedundancy;
        var hasMonitoring = input.InputMonitoring || input.LogicMonitoring || input.OutputMonitoring;
        var hasTestEquipment = input.TestEquipment;
        var ccfScore = input.CcfScore ?? 0;

        // 根据特征推荐类别
        if (hasRedundancy && hasMonitoring && ccfScore >= 65)
        {
            result.Suggestions.Add(new CategorySuggestion
            {
                Category = "Cat4",
                Confidence = 0.9,
                Reason = "检测到冗余通道、监测功能和CCF评分≥65，符合Category 4要求"
            });
        }
        else if (hasRedundancy && hasMonitoring)
        {
            result.Suggestions.Add(new CategorySuggestion
            {
                Category = "Cat3",
                Confidence = 0.8,
                Reason = "检测到冗余通道和监测功能，建议Category 3"
            });

            if (ccfScore < 65)
            {
                result.Conflicts.Add("Category 3 要求CCF评分≥65，当前评分不足");
                result.Recommendations.Add("增加CCF措施以提高评分至65分以上");
            }
        }
        else if (hasTestEquipment)
        {
            result.Suggestions.Add(new CategorySuggestion
            {
                Category = "Cat2",
                Confidence = 0.7,
                Reason = "检测到测试设备，建议Category 2"
            });
        }
        else
        {
            result.Suggestions.Add(new CategorySuggestion
            {
                Category = "Cat1",
                Confidence = 0.6,
                Reason = "单通道无监测，建议Category 1"
            });

            if (input.RequiredPL == "PLd" || input.RequiredPL == "PLe")
            {
                result.Conflicts.Add($"所需PL为{input.RequiredPL}，但Category 1无法达到该要求");
                result.Recommendations.Add("建议增加冗余通道或监测功能以提升类别");
            }
        }

        // 检查冲突
        if (input.SelectedCategory == "Cat3" && ccfScore < 65)
        {
            result.Conflicts.Add("Category 3 要求CCF评分≥65");
        }

        if (input.SelectedCategory == "Cat4" && (!hasRedundancy || !hasMonitoring || ccfScore < 65))
        {
            result.Conflicts.Add("Category 4 要求冗余通道、监测功能和CCF评分≥65");
        }

        return result;
    }

    /// <summary>
    /// 检查串联设备数量限制
    /// </summary>
    public SeriesDeviceCheckResult CheckSeriesDeviceLimit(int seriesCount, double targetDcavg)
    {
        var result = new SeriesDeviceCheckResult
        {
            SeriesCount = seriesCount,
            IsWithinLimit = true,
            Warnings = new List<string>()
        };

        // Annex K 建议：串联设备数量应有限制
        var recommendedMax = 10;
        if (seriesCount > recommendedMax)
        {
            result.IsWithinLimit = false;
            result.Warnings.Add($"⚠️ 串联设备数量 ({seriesCount}) 超过推荐上限 ({recommendedMax})");
            result.Warnings.Add("过多的串联设备会增加故障掩蔽风险，建议重新设计架构");
        }

        // 检查DCavg是否因设备数量过多而受限
        var estimatedLimit = CalculateMaskingLimit(seriesCount, 1.0);
        if (targetDcavg > estimatedLimit)
        {
            result.Warnings.Add($"目标DCavg ({targetDcavg:P2}) 可能因设备数量过多而无法达到");
            result.Warnings.Add($"建议的最大DCavg约为 {estimatedLimit:P2}");
        }

        return result;
    }
}

public class DeviceDcavgInfo
{
    public string Id { get; set; } = string.Empty;
    public double Dcavg { get; set; }
}

public class DcavgCalculationResult
{
    public double Dcavg { get; set; }
    public string Method { get; set; } = string.Empty;
    public double MaskingLimit { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class CategorySelectionInput
{
    public bool InputRedundancy { get; set; }
    public bool LogicRedundancy { get; set; }
    public bool OutputRedundancy { get; set; }
    public bool InputMonitoring { get; set; }
    public bool LogicMonitoring { get; set; }
    public bool OutputMonitoring { get; set; }
    public bool TestEquipment { get; set; }
    public int? CcfScore { get; set; }
    public string RequiredPL { get; set; } = string.Empty;
    public string SelectedCategory { get; set; } = string.Empty;
}

public class CategorySelectionResult
{
    public List<CategorySuggestion> Suggestions { get; set; } = new();
    public List<string> Conflicts { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

// CategorySuggestion定义在CategoryDerivationEnhancementService中

public class SeriesDeviceCheckResult
{
    public int SeriesCount { get; set; }
    public bool IsWithinLimit { get; set; }
    public List<string> Warnings { get; set; } = new();
}

