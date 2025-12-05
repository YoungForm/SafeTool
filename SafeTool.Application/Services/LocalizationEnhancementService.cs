using System.Globalization;

namespace SafeTool.Application.Services;

/// <summary>
/// 本地化增强服务（单位制本地化、格式本地化）
/// </summary>
public class LocalizationEnhancementService
{
    private readonly ILocalizationService _baseService;

    public LocalizationEnhancementService(ILocalizationService baseService)
    {
        _baseService = baseService;
    }

    /// <summary>
    /// 格式化单位（单位制本地化）
    /// </summary>
    public string FormatUnit(double value, string unit, string language = "zh-CN")
    {
        var formattedValue = _baseService.FormatNumber(value, language);
        
        // 单位本地化
        var localizedUnit = GetLocalizedUnit(unit, language);
        
        // 根据语言决定单位位置
        if (language.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return $"{formattedValue} {localizedUnit}";
        }
        else
        {
            return $"{formattedValue} {localizedUnit}";
        }
    }

    /// <summary>
    /// 格式化时间单位
    /// </summary>
    public string FormatTimeUnit(double hours, string language = "zh-CN")
    {
        if (language.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            if (hours < 1)
            {
                var minutes = hours * 60;
                return $"{minutes:F0} 分钟";
            }
            else if (hours < 24)
            {
                return $"{hours:F2} 小时";
            }
            else if (hours < 8760)
            {
                var days = hours / 24;
                return $"{days:F1} 天";
            }
            else
            {
                var years = hours / 8760;
                return $"{years:F2} 年";
            }
        }
        else
        {
            if (hours < 1)
            {
                var minutes = hours * 60;
                return $"{minutes:F0} min";
            }
            else if (hours < 24)
            {
                return $"{hours:F2} h";
            }
            else if (hours < 8760)
            {
                var days = hours / 24;
                return $"{days:F1} days";
            }
            else
            {
                var years = hours / 8760;
                return $"{years:F2} years";
            }
        }
    }

    /// <summary>
    /// 格式化频率单位
    /// </summary>
    public string FormatFrequency(double frequency, string language = "zh-CN")
    {
        var formattedValue = _baseService.FormatNumber(frequency, language);
        
        if (language.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return $"{formattedValue} 次/小时";
        }
        else
        {
            return $"{formattedValue} per hour";
        }
    }

    /// <summary>
    /// 格式化百分比
    /// </summary>
    public string FormatPercentage(double value, string language = "zh-CN")
    {
        var formattedValue = _baseService.FormatNumber(value * 100, language);
        return $"{formattedValue}%";
    }

    /// <summary>
    /// 格式化日期时间（格式本地化）
    /// </summary>
    public string FormatDateTimeLocalized(DateTime dateTime, string format, string language = "zh-CN")
    {
        var culture = GetCulture(language);
        
        // 自定义格式映射
        var formatMap = new Dictionary<string, string>
        {
            { "short", language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "yyyy-MM-dd" : "MM/dd/yyyy" },
            { "long", language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "yyyy年MM月dd日" : "MMMM dd, yyyy" },
            { "full", language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "yyyy年MM月dd日 HH:mm:ss" : "MMMM dd, yyyy HH:mm:ss" }
        };

        var actualFormat = formatMap.GetValueOrDefault(format, format);
        return dateTime.ToString(actualFormat, culture);
    }

    /// <summary>
    /// 格式化数字（格式本地化）
    /// </summary>
    public string FormatNumberLocalized(double number, string format, string language = "zh-CN")
    {
        var culture = GetCulture(language);
        
        // 格式映射
        var formatMap = new Dictionary<string, string>
        {
            { "integer", "N0" },
            { "decimal", "N2" },
            { "scientific", "E2" },
            { "currency", "C2" }
        };

        var actualFormat = formatMap.GetValueOrDefault(format, format);
        return number.ToString(actualFormat, culture);
    }

    /// <summary>
    /// 获取所有本地化字符串（用于前端UI）
    /// </summary>
    public Dictionary<string, string> GetAllLocalizations(string language = "zh-CN")
    {
        var localizations = _baseService.GetLocalizations(language);
        
        // 添加增强的本地化字符串
        var enhanced = new Dictionary<string, string>(localizations);
        
        // UI相关字符串
        if (language.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            enhanced["Language"] = "语言";
            enhanced["Chinese"] = "中文";
            enhanced["English"] = "英文";
            enhanced["Save"] = "保存";
            enhanced["Cancel"] = "取消";
            enhanced["Delete"] = "删除";
            enhanced["Edit"] = "编辑";
            enhanced["Add"] = "添加";
            enhanced["Search"] = "搜索";
            enhanced["Filter"] = "筛选";
            enhanced["Export"] = "导出";
            enhanced["Import"] = "导入";
            enhanced["Settings"] = "设置";
            enhanced["Help"] = "帮助";
        }
        else
        {
            enhanced["Language"] = "Language";
            enhanced["Chinese"] = "Chinese";
            enhanced["English"] = "English";
            enhanced["Save"] = "Save";
            enhanced["Cancel"] = "Cancel";
            enhanced["Delete"] = "Delete";
            enhanced["Edit"] = "Edit";
            enhanced["Add"] = "Add";
            enhanced["Search"] = "Search";
            enhanced["Filter"] = "Filter";
            enhanced["Export"] = "Export";
            enhanced["Import"] = "Import";
            enhanced["Settings"] = "Settings";
            enhanced["Help"] = "Help";
        }
        
        return enhanced;
    }

    /// <summary>
    /// 获取支持的语言列表
    /// </summary>
    public List<LanguageInfo> GetSupportedLanguages()
    {
        return new List<LanguageInfo>
        {
            new LanguageInfo
            {
                Code = "zh-CN",
                Name = "中文（简体）",
                NativeName = "中文（简体）",
                Culture = "zh-CN"
            },
            new LanguageInfo
            {
                Code = "en-US",
                Name = "English (United States)",
                NativeName = "English (United States)",
                Culture = "en-US"
            }
        };
    }

    private string GetLocalizedUnit(string unit, string language)
    {
        var unitMap = new Dictionary<string, Dictionary<string, string>>
        {
            ["hour"] = new Dictionary<string, string>
            {
                ["zh-CN"] = "小时",
                ["en-US"] = "hour"
            },
            ["day"] = new Dictionary<string, string>
            {
                ["zh-CN"] = "天",
                ["en-US"] = "day"
            },
            ["year"] = new Dictionary<string, string>
            {
                ["zh-CN"] = "年",
                ["en-US"] = "year"
            },
            ["ampere"] = new Dictionary<string, string>
            {
                ["zh-CN"] = "安培",
                ["en-US"] = "A"
            },
            ["volt"] = new Dictionary<string, string>
            {
                ["zh-CN"] = "伏特",
                ["en-US"] = "V"
            },
            ["watt"] = new Dictionary<string, string>
            {
                ["zh-CN"] = "瓦特",
                ["en-US"] = "W"
            }
        };

        if (unitMap.TryGetValue(unit.ToLower(), out var translations))
        {
            return translations.GetValueOrDefault(language, unit);
        }

        return unit;
    }

    private CultureInfo GetCulture(string language)
    {
        try
        {
            return new CultureInfo(language);
        }
        catch
        {
            return new CultureInfo("zh-CN");
        }
    }
}

public class LanguageInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string Culture { get; set; } = string.Empty;
}

