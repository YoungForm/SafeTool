namespace SafeTool.Application.Services;

/// <summary>
/// 本地化服务接口（策略模式）
/// </summary>
public interface ILocalizationService
{
    string GetString(string key, string language = "zh-CN");
    string Localize(string text, string language = "zh-CN");
    Dictionary<string, string> GetLocalizations(string language = "zh-CN");
    string FormatNumber(double number, string language = "zh-CN");
    string FormatDate(DateTime date, string language = "zh-CN");
    string FormatDateTime(DateTime dateTime, string language = "zh-CN");
}

