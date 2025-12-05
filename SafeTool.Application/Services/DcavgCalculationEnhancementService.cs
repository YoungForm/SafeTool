namespace SafeTool.Application.Services;

/// <summary>
/// DCavg计算增强服务（Annex K完整实现）
/// </summary>
public class DcavgCalculationEnhancementService
{
    private readonly Iso13849CalculationEnhancementService _baseService;

    public DcavgCalculationEnhancementService(Iso13849CalculationEnhancementService baseService)
    {
        _baseService = baseService;
    }

    /// <summary>
    /// 增强的DCavg常规法计算（Annex K完整实现）
    /// </summary>
    public EnhancedDcavgResult CalculateDcavgEnhanced(EnhancedDcavgInput input)
    {
        var result = new EnhancedDcavgResult
        {
            Method = "regular-enhanced",
            Warnings = new List<string>(),
            Recommendations = new List<string>(),
            CalculationSteps = new List<CalculationStep>()
        };

        // 步骤1：验证输入
        ValidateInput(input, result);

        // 步骤2：计算各设备的DC贡献
        var deviceContributions = CalculateDeviceContributions(input.Devices, result);

        // 步骤3：计算测试设备DC
        var testDc = CalculateTestDc(input.TestParameters, input.DemandRate, result);

        // 步骤4：计算串联DCavg
        var seriesDcavg = CalculateSeriesDcavg(deviceContributions, testDc, result);

        // 步骤5：检查故障掩蔽上限
        var maskingLimit = _baseService.CalculateDcavgRegular(
            input.Devices.Select(d => new DeviceDcavgInfo { Id = d.Id, Dcavg = d.Dcavg }).ToList(),
            input.DemandRate,
            input.Devices.Count).MaskingLimit;

        result.Dcavg = Math.Min(seriesDcavg, maskingLimit);
        result.MaskingLimit = maskingLimit;

        if (seriesDcavg > maskingLimit)
        {
            result.Warnings.Add($"⚠️ DCavg ({seriesDcavg:P2}) 超过故障掩蔽上限 ({maskingLimit:P2})");
            result.Warnings.Add("已应用上限限制");
            result.Recommendations.Add("减少串联设备数量或提高测试频率");
        }

        // 步骤6：生成优化建议
        GenerateOptimizationSuggestions(input, result);

        return result;
    }

    private void ValidateInput(EnhancedDcavgInput input, EnhancedDcavgResult result)
    {
        result.CalculationSteps.Add(new CalculationStep
        {
            Step = 1,
            Description = "输入验证",
            Details = new List<string>()
        });

        if (input.Devices.Count == 0)
        {
            result.Warnings.Add("⚠️ 未提供设备信息");
            return;
        }

        foreach (var device in input.Devices)
        {
            if (device.Dcavg < 0 || device.Dcavg > 1)
            {
                result.Warnings.Add($"⚠️ 设备 {device.Id} 的DCavg值 {device.Dcavg} 超出有效范围 [0,1]");
            }
        }

        if (input.DemandRate < 0 || input.DemandRate > 1)
        {
            result.Warnings.Add($"⚠️ 需求率 {input.DemandRate} 超出有效范围 [0,1]");
        }
    }

    private List<DeviceContribution> CalculateDeviceContributions(
        List<DeviceDcavgInfo> devices,
        EnhancedDcavgResult result)
    {
        result.CalculationSteps.Add(new CalculationStep
        {
            Step = 2,
            Description = "计算各设备DC贡献",
            Details = new List<string>()
        });

        var contributions = new List<DeviceContribution>();
        double product = 1.0;

        foreach (var device in devices)
        {
            var contribution = new DeviceContribution
            {
                DeviceId = device.Id,
                Dcavg = device.Dcavg,
                Contribution = 1 - device.Dcavg
            };
            contributions.Add(contribution);
            product *= contribution.Contribution;

            result.CalculationSteps.Last().Details.Add(
                $"设备 {device.Id}: DCavg={device.Dcavg:P2}, 贡献={contribution.Contribution:P2}");
        }

        result.IntermediateProduct = product;
        return contributions;
    }

    private double CalculateTestDc(
        TestParameters? testParams,
        double demandRate,
        EnhancedDcavgResult result)
    {
        result.CalculationSteps.Add(new CalculationStep
        {
            Step = 3,
            Description = "计算测试设备DC",
            Details = new List<string>()
        });

        double testDc = 0.0;

        if (testParams != null)
        {
            // 基于测试频率和覆盖率计算
            if (testParams.TestFrequency > 0 && testParams.TestCoverage > 0)
            {
                // 测试DC = 测试覆盖率 * (1 - e^(-需求率 * 测试频率))
                var testEffectiveness = 1 - Math.Exp(-demandRate * testParams.TestFrequency);
                testDc = testParams.TestCoverage * testEffectiveness;
                testDc = Math.Min(0.99, testDc);

                result.CalculationSteps.Last().Details.Add(
                    $"测试频率={testParams.TestFrequency}, 测试覆盖率={testParams.TestCoverage:P2}");
                result.CalculationSteps.Last().Details.Add(
                    $"测试有效性={testEffectiveness:P2}, 测试DC={testDc:P2}");
            }
        }
        else if (demandRate > 0)
        {
            // 简化计算
            testDc = Math.Min(0.99, demandRate * 0.1);
            result.CalculationSteps.Last().Details.Add(
                $"使用简化计算: 测试DC={testDc:P2}");
        }

        return testDc;
    }

    private double CalculateSeriesDcavg(
        List<DeviceContribution> contributions,
        double testDc,
        EnhancedDcavgResult result)
    {
        result.CalculationSteps.Add(new CalculationStep
        {
            Step = 4,
            Description = "计算串联DCavg",
            Details = new List<string>()
        });

        // DCavg = 1 - (1-DC1) * (1-DC2) * ... * (1-DCn) * (1-DCtest)
        var product = contributions.Aggregate(1.0, (acc, c) => acc * c.Contribution);
        product *= (1 - testDc);

        var dcavg = 1 - product;

        result.CalculationSteps.Last().Details.Add(
            $"产品项={product:P4}, 最终DCavg={dcavg:P2}");

        return dcavg;
    }

    private void GenerateOptimizationSuggestions(
        EnhancedDcavgInput input,
        EnhancedDcavgResult result)
    {
        result.CalculationSteps.Add(new CalculationStep
        {
            Step = 5,
            Description = "生成优化建议",
            Details = new List<string>()
        });

        // 检查低DCavg设备
        var lowDcDevices = input.Devices.Where(d => d.Dcavg < 0.6).ToList();
        if (lowDcDevices.Any())
        {
            result.Recommendations.Add(
                $"发现 {lowDcDevices.Count} 个低DCavg设备（<60%），建议提高其诊断覆盖率");
            foreach (var device in lowDcDevices)
            {
                result.Recommendations.Add($"  - 设备 {device.Id}: DCavg={device.Dcavg:P2}");
            }
        }

        // 检查串联设备数量
        if (input.Devices.Count > 5)
        {
            result.Recommendations.Add(
                $"串联设备数量 ({input.Devices.Count}) 较多，建议重新设计架构以减少串联设备");
        }

        // 检查测试参数
        if (input.TestParameters == null || input.TestParameters.TestCoverage < 0.9)
        {
            result.Recommendations.Add("建议提高测试覆盖率至90%以上");
        }
    }
}

public class EnhancedDcavgInput
{
    public List<DeviceDcavgInfo> Devices { get; set; } = new();
    public double DemandRate { get; set; } = 1.0;
    public TestParameters? TestParameters { get; set; }
}

public class TestParameters
{
    public double TestFrequency { get; set; } // 测试频率（次/小时）
    public double TestCoverage { get; set; } // 测试覆盖率（0-1）
}

public class EnhancedDcavgResult
{
    public double Dcavg { get; set; }
    public string Method { get; set; } = string.Empty;
    public double MaskingLimit { get; set; }
    public double? IntermediateProduct { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<CalculationStep> CalculationSteps { get; set; } = new();
}

public class CalculationStep
{
    public int Step { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Details { get; set; } = new();
}

public class DeviceContribution
{
    public string DeviceId { get; set; } = string.Empty;
    public double Dcavg { get; set; }
    public double Contribution { get; set; }
}

