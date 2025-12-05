using System.Text.RegularExpressions;

namespace SafeTool.Application.Services;

/// <summary>
/// 数据脱敏服务（策略模式）
/// </summary>
public class DataMaskingService
{
    /// <summary>
    /// 脱敏策略接口
    /// </summary>
    public interface IMaskingStrategy
    {
        string Mask(string value);
    }

    /// <summary>
    /// 默认脱敏策略
    /// </summary>
    public class DefaultMaskingStrategy : IMaskingStrategy
    {
        public string Mask(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= 4)
                return "****";
            
            if (value.Length <= 8)
                return value.Substring(0, 2) + "****" + value.Substring(value.Length - 2);
            
            return value.Substring(0, 4) + "****" + value.Substring(value.Length - 4);
        }
    }

    /// <summary>
    /// 邮箱脱敏策略
    /// </summary>
    public class EmailMaskingStrategy : IMaskingStrategy
    {
        public string Mask(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
                return "****@****";
            
            var parts = value.Split('@');
            var username = parts[0];
            var domain = parts[1];
            
            var maskedUsername = username.Length > 2 
                ? username.Substring(0, 2) + "***" 
                : "***";
            
            return $"{maskedUsername}@{domain}";
        }
    }

    /// <summary>
    /// 数字脱敏策略（用于PFHd等敏感参数）
    /// </summary>
    public class NumberMaskingStrategy : IMaskingStrategy
    {
        public string Mask(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "****";
            
            // 对于科学计数法格式（如 1e-7）
            if (value.Contains('e') || value.Contains('E'))
            {
                var parts = value.ToLower().Split('e');
                if (parts.Length == 2)
                {
                    var baseValue = parts[0];
                    var exponent = parts[1];
                    return $"{MaskNumber(baseValue)}e{exponent}";
                }
            }
            
            return MaskNumber(value);
        }

        private string MaskNumber(string value)
        {
            if (value.Length <= 4)
                return "****";
            
            return value.Substring(0, 2) + "***" + value.Substring(value.Length - 2);
        }
    }

    private readonly Dictionary<string, IMaskingStrategy> _strategies = new();

    public DataMaskingService()
    {
        _strategies["default"] = new DefaultMaskingStrategy();
        _strategies["email"] = new EmailMaskingStrategy();
        _strategies["number"] = new NumberMaskingStrategy();
    }

    /// <summary>
    /// 脱敏敏感参数
    /// </summary>
    public Dictionary<string, string> MaskSensitiveParameters(Dictionary<string, string> parameters, IEnumerable<string> sensitiveKeys)
    {
        var masked = new Dictionary<string, string>(parameters);
        var sensitiveSet = new HashSet<string>(sensitiveKeys, StringComparer.OrdinalIgnoreCase);

        foreach (var key in masked.Keys.ToList())
        {
            if (sensitiveSet.Contains(key))
            {
                var strategy = DetermineStrategy(key, masked[key]);
                masked[key] = strategy.Mask(masked[key]);
            }
        }

        return masked;
    }

    /// <summary>
    /// 脱敏组件参数
    /// </summary>
    public ComponentLibraryService.ComponentRecord MaskComponentParameters(
        ComponentLibraryService.ComponentRecord component,
        bool hasPermission)
    {
        if (hasPermission)
            return component;

        var masked = new ComponentLibraryService.ComponentRecord
        {
            Id = component.Id,
            Manufacturer = component.Manufacturer,
            Model = component.Model,
            Category = component.Category,
            Parameters = new Dictionary<string, string>()
        };

        // 敏感参数列表
        var sensitiveParams = new[] { "PFHd", "pfhd", "B10d", "b10d", "MTTFd", "mttfd", "DCavg", "dcavg", "beta", "Beta" };

        foreach (var kvp in component.Parameters)
        {
            if (sensitiveParams.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                var strategy = DetermineStrategy(kvp.Key, kvp.Value);
                masked.Parameters[kvp.Key] = strategy.Mask(kvp.Value);
            }
            else
            {
                masked.Parameters[kvp.Key] = kvp.Value;
            }
        }

        return masked;
    }

    /// <summary>
    /// 确定脱敏策略
    /// </summary>
    private IMaskingStrategy DetermineStrategy(string key, string value)
    {
        if (key.Contains("email", StringComparison.OrdinalIgnoreCase) || value.Contains('@'))
            return _strategies["email"];
        
        if (IsNumeric(value))
            return _strategies["number"];
        
        return _strategies["default"];
    }

    private bool IsNumeric(string value)
    {
        return double.TryParse(value, System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, out _) ||
               Regex.IsMatch(value, @"^[\d\.eE\+\-]+$");
    }

    /// <summary>
    /// 脱敏用户信息
    /// </summary>
    public string MaskUserInfo(string userInfo)
    {
        var strategy = _strategies["default"];
        return strategy.Mask(userInfo);
    }
}

