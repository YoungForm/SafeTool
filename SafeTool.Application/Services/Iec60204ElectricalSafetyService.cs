namespace SafeTool.Application.Services;

/// <summary>
/// IEC 60204-1 电气安全检查服务（策略模式）
/// </summary>
public class Iec60204ElectricalSafetyService
{
    /// <summary>
    /// 过载保护检查（条款7.2）
    /// </summary>
    public OverloadProtectionCheckResult CheckOverloadProtection(OverloadProtectionInput input)
    {
        var result = new OverloadProtectionCheckResult
        {
            MotorRatedCurrent = input.MotorRatedCurrent,
            ProtectionDeviceType = input.ProtectionDeviceType,
            ProtectionDeviceRating = input.ProtectionDeviceRating,
            Warnings = new List<string>(),
            Recommendations = new List<string>(),
            IsCompliant = true
        };

        // 检查保护装置类型
        if (string.IsNullOrWhiteSpace(input.ProtectionDeviceType))
        {
            result.IsCompliant = false;
            result.Warnings.Add("⚠️ 缺少过载保护装置类型信息");
            result.Recommendations.Add("必须安装过载保护装置（热继电器、电子过载继电器或熔断器）");
        }

        // 检查保护装置整定值
        if (input.ProtectionDeviceRating.HasValue && input.MotorRatedCurrent > 0)
        {
            var ratio = input.ProtectionDeviceRating.Value / input.MotorRatedCurrent;
            
            // IEC 60204-1要求：保护装置整定值应在电动机额定电流的100%-125%之间
            if (ratio < 1.0)
            {
                result.IsCompliant = false;
                result.Warnings.Add($"⚠️ 严重：保护装置整定值（{input.ProtectionDeviceRating.Value}A）低于电动机额定电流（{input.MotorRatedCurrent}A）");
                result.Recommendations.Add("保护装置整定值必须至少等于电动机额定电流");
            }
            else if (ratio > 1.25)
            {
                result.IsCompliant = false;
                result.Warnings.Add($"⚠️ 警告：保护装置整定值（{input.ProtectionDeviceRating.Value}A）超过电动机额定电流的125%（{input.MotorRatedCurrent * 1.25:F2}A）");
                result.Recommendations.Add("保护装置整定值不应超过电动机额定电流的125%");
                result.Recommendations.Add($"建议整定值范围：{input.MotorRatedCurrent:F2}A - {input.MotorRatedCurrent * 1.25:F2}A");
            }
            else
            {
                result.Warnings.Add($"✓ 保护装置整定值（{input.ProtectionDeviceRating.Value}A）在允许范围内（{ratio:P0}）");
            }
        }
        else
        {
            result.Warnings.Add("⚠️ 缺少保护装置整定值或电动机额定电流信息");
            result.Recommendations.Add("必须提供保护装置整定值和电动机额定电流以进行校核");
        }

        // 检查保护装置类型适用性
        if (!string.IsNullOrWhiteSpace(input.ProtectionDeviceType))
        {
            var deviceType = input.ProtectionDeviceType.ToLower();
            if (deviceType.Contains("热继电器") || deviceType.Contains("thermal"))
            {
                result.Warnings.Add("✓ 热继电器适用于大多数应用场景");
                result.Recommendations.Add("确保热继电器与电动机特性匹配");
            }
            else if (deviceType.Contains("电子") || deviceType.Contains("electronic"))
            {
                result.Warnings.Add("✓ 电子过载继电器提供更精确的保护");
                result.Recommendations.Add("电子过载继电器适用于需要精确保护的场合");
            }
            else if (deviceType.Contains("熔断器") || deviceType.Contains("fuse"))
            {
                result.Warnings.Add("⚠️ 注意：熔断器主要用于短路保护，过载保护能力有限");
                result.Recommendations.Add("建议使用热继电器或电子过载继电器进行过载保护");
                result.Recommendations.Add("熔断器应配合其他过载保护装置使用");
            }
        }

        // 检查多电动机保护
        if (input.MotorCount > 1)
        {
            result.Warnings.Add($"注意：系统包含 {input.MotorCount} 个电动机");
            result.Recommendations.Add("每个电动机应单独配置过载保护装置");
            result.Recommendations.Add("检查总保护装置是否满足要求");
        }

        return result;
    }

    /// <summary>
    /// 隔离与短路保护检查（条款5.3和7.2）
    /// </summary>
    public IsolationAndShortCircuitCheckResult CheckIsolationAndShortCircuit(IsolationAndShortCircuitInput input)
    {
        var result = new IsolationAndShortCircuitCheckResult
        {
            HasMainSwitch = input.HasMainSwitch,
            HasIsolator = input.HasIsolator,
            ShortCircuitProtectionType = input.ShortCircuitProtectionType,
            ShortCircuitRating = input.ShortCircuitRating,
            Warnings = new List<string>(),
            Recommendations = new List<string>(),
            IsCompliant = true
        };

        // 检查主开关/隔离器
        if (!input.HasMainSwitch && !input.HasIsolator)
        {
            result.IsCompliant = false;
            result.Warnings.Add("⚠️ 严重：缺少主开关或隔离器");
            result.Recommendations.Add("必须安装主开关或隔离器以实现安全隔离（IEC 60204-1 条款5.3）");
        }
        else if (input.HasMainSwitch && input.HasIsolator)
        {
            result.Warnings.Add("✓ 同时配置主开关和隔离器，符合最佳实践");
        }
        else if (input.HasMainSwitch)
        {
            result.Warnings.Add("✓ 已配置主开关");
            result.Recommendations.Add("确保主开关满足隔离要求（可见的断开点、锁定功能）");
        }
        else
        {
            result.Warnings.Add("✓ 已配置隔离器");
            result.Recommendations.Add("确保隔离器满足断开要求（可见的断开点、锁定功能）");
        }

        // 检查短路保护
        if (string.IsNullOrWhiteSpace(input.ShortCircuitProtectionType))
        {
            result.IsCompliant = false;
            result.Warnings.Add("⚠️ 严重：缺少短路保护装置信息");
            result.Recommendations.Add("必须安装短路保护装置（熔断器或断路器）");
        }
        else
        {
            var deviceType = input.ShortCircuitProtectionType.ToLower();
            if (deviceType.Contains("熔断器") || deviceType.Contains("fuse"))
            {
                result.Warnings.Add("✓ 使用熔断器进行短路保护");
                result.Recommendations.Add("确保熔断器额定电流和分断能力满足要求");
                result.Recommendations.Add("检查熔断器与过载保护的协调性");
            }
            else if (deviceType.Contains("断路器") || deviceType.Contains("circuit breaker"))
            {
                result.Warnings.Add("✓ 使用断路器进行短路保护");
                result.Recommendations.Add("确保断路器额定电流和分断能力满足要求");
                result.Recommendations.Add("检查断路器与过载保护的协调性");
            }
        }

        // 检查短路保护整定值
        if (input.ShortCircuitRating.HasValue && input.LoadCurrent > 0)
        {
            var ratio = input.ShortCircuitRating.Value / input.LoadCurrent;
            
            // 短路保护装置应能承受预期短路电流
            if (input.ExpectedShortCircuitCurrent.HasValue)
            {
                if (input.ShortCircuitRating.Value < input.ExpectedShortCircuitCurrent.Value)
                {
                    result.IsCompliant = false;
                    result.Warnings.Add($"⚠️ 严重：短路保护装置分断能力（{input.ShortCircuitRating.Value}kA）低于预期短路电流（{input.ExpectedShortCircuitCurrent.Value}kA）");
                    result.Recommendations.Add($"必须选择分断能力至少为 {input.ExpectedShortCircuitCurrent.Value}kA 的保护装置");
                }
                else
                {
                    result.Warnings.Add($"✓ 短路保护装置分断能力（{input.ShortCircuitRating.Value}kA）满足要求");
                }
            }
            else
            {
                result.Warnings.Add("⚠️ 缺少预期短路电流信息");
                result.Recommendations.Add("应进行短路电流计算以确定保护装置分断能力要求");
            }
        }

        // 检查保护协调
        if (input.HasOverloadProtection && !string.IsNullOrWhiteSpace(input.ShortCircuitProtectionType))
        {
            result.Warnings.Add("✓ 同时配置过载保护和短路保护");
            result.Recommendations.Add("确保过载保护和短路保护的协调性（选择性保护）");
            result.Recommendations.Add("检查保护装置的时间-电流特性曲线");
        }

        // 检查锁定功能
        if (input.HasLockingCapability)
        {
            result.Warnings.Add("✓ 隔离装置具有锁定功能，符合安全要求");
        }
        else
        {
            result.Warnings.Add("⚠️ 注意：隔离装置缺少锁定功能");
            result.Recommendations.Add("建议安装锁定装置以防止意外接通（IEC 60204-1 条款5.3.2）");
        }

        return result;
    }

    /// <summary>
    /// 综合电气安全检查
    /// </summary>
    public ComprehensiveElectricalSafetyResult ComprehensiveCheck(
        OverloadProtectionInput? overloadInput = null,
        IsolationAndShortCircuitInput? isolationInput = null)
    {
        var result = new ComprehensiveElectricalSafetyResult
        {
            Warnings = new List<string>(),
            Recommendations = new List<string>(),
            IsCompliant = true
        };

        if (overloadInput != null)
        {
            var overloadResult = CheckOverloadProtection(overloadInput);
            result.OverloadProtectionResult = overloadResult;
            result.Warnings.AddRange(overloadResult.Warnings);
            result.Recommendations.AddRange(overloadResult.Recommendations);
            if (!overloadResult.IsCompliant)
                result.IsCompliant = false;
        }

        if (isolationInput != null)
        {
            var isolationResult = CheckIsolationAndShortCircuit(isolationInput);
            result.IsolationAndShortCircuitResult = isolationResult;
            result.Warnings.AddRange(isolationResult.Warnings);
            result.Recommendations.AddRange(isolationResult.Recommendations);
            if (!isolationResult.IsCompliant)
                result.IsCompliant = false;
        }

        return result;
    }
}

public class OverloadProtectionInput
{
    public double MotorRatedCurrent { get; set; } // 电动机额定电流（A）
    public string? ProtectionDeviceType { get; set; } // 保护装置类型（热继电器/电子过载继电器/熔断器）
    public double? ProtectionDeviceRating { get; set; } // 保护装置整定值（A）
    public int MotorCount { get; set; } = 1; // 电动机数量
}

public class OverloadProtectionCheckResult
{
    public double MotorRatedCurrent { get; set; }
    public string? ProtectionDeviceType { get; set; }
    public double? ProtectionDeviceRating { get; set; }
    public bool IsCompliant { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class IsolationAndShortCircuitInput
{
    public bool HasMainSwitch { get; set; } // 是否有主开关
    public bool HasIsolator { get; set; } // 是否有隔离器
    public bool HasLockingCapability { get; set; } // 是否有锁定功能
    public string? ShortCircuitProtectionType { get; set; } // 短路保护装置类型（熔断器/断路器）
    public double? ShortCircuitRating { get; set; } // 短路保护装置分断能力（kA）
    public double LoadCurrent { get; set; } // 负载电流（A）
    public double? ExpectedShortCircuitCurrent { get; set; } // 预期短路电流（kA）
    public bool HasOverloadProtection { get; set; } // 是否有过载保护
}

public class IsolationAndShortCircuitCheckResult
{
    public bool HasMainSwitch { get; set; }
    public bool HasIsolator { get; set; }
    public string? ShortCircuitProtectionType { get; set; }
    public double? ShortCircuitRating { get; set; }
    public bool IsCompliant { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class ComprehensiveElectricalSafetyResult
{
    public OverloadProtectionCheckResult? OverloadProtectionResult { get; set; }
    public IsolationAndShortCircuitCheckResult? IsolationAndShortCircuitResult { get; set; }
    public bool IsCompliant { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

