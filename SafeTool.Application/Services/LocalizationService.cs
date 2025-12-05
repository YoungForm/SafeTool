using System.Globalization;

namespace SafeTool.Application.Services;

/// <summary>
/// 本地化服务实现（策略模式）
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations = new();
    private readonly Dictionary<string, CultureInfo> _cultures = new();

    public LocalizationService()
    {
        InitializeTranslations();
        InitializeCultures();
    }

    private void InitializeTranslations()
    {
        // 中文翻译
        var zhCN = new Dictionary<string, string>
        {
            { "Title", "合规自检报告" },
            { "SystemLabel", "系统" },
            { "AssessorLabel", "评估人" },
            { "DateLabel", "日期" },
            { "ConclusionLabel", "结论" },
            { "GeneratedAtLabel", "生成时间" },
            { "ComplianceReport", "合规自检报告" },
            { "SRSDocument", "安全需求规格" },
            { "IEC62061Report", "IEC 62061 评估报告" },
            { "CombinedReport", "合并报告" }
        };
        _translations["zh-CN"] = zhCN;

        // 英文翻译
        var enUS = new Dictionary<string, string>
        {
            { "Title", "Compliance Report" },
            { "SystemLabel", "System" },
            { "AssessorLabel", "Assessor" },
            { "DateLabel", "Date" },
            { "ConclusionLabel", "Conclusion" },
            { "GeneratedAtLabel", "Generated At" },
            { "ComplianceReport", "Compliance Report" },
            { "SRSDocument", "Safety Requirements Specification" },
            { "IEC62061Report", "IEC 62061 Evaluation Report" },
            { "CombinedReport", "Combined Report" }
        };
        _translations["en-US"] = enUS;
    }

    private void InitializeCultures()
    {
        _cultures["zh-CN"] = new CultureInfo("zh-CN");
        _cultures["en-US"] = new CultureInfo("en-US");
    }

    public string GetString(string key, string language = "zh-CN")
    {
        if (_translations.TryGetValue(language, out var dict) && dict.TryGetValue(key, out var value))
            return value;
        
        // 回退到中文
        if (_translations.TryGetValue("zh-CN", out var zhDict) && zhDict.TryGetValue(key, out var zhValue))
            return zhValue;
        
        return key;
    }

    public string Localize(string text, string language = "zh-CN")
    {
        // 简单的本地化：替换已知的键
        var result = text;
        var translations = _translations.TryGetValue(language, out var dict) ? dict : _translations["zh-CN"];
        
        foreach (var kvp in translations)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value);
        }
        
        return result;
    }

    public Dictionary<string, string> GetLocalizations(string language = "zh-CN")
    {
        return _translations.TryGetValue(language, out var dict) ? dict : _translations["zh-CN"];
    }

    public string FormatNumber(double number, string language = "zh-CN")
    {
        var culture = _cultures.TryGetValue(language, out var c) ? c : _cultures["zh-CN"];
        return number.ToString("N2", culture);
    }

    public string FormatDate(DateTime date, string language = "zh-CN")
    {
        var culture = _cultures.TryGetValue(language, out var c) ? c : _cultures["zh-CN"];
        return date.ToString("yyyy-MM-dd", culture);
    }

    public string FormatDateTime(DateTime dateTime, string language = "zh-CN")
    {
        var culture = _cultures.TryGetValue(language, out var c) ? c : _cultures["zh-CN"];
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss", culture);
    }
}

