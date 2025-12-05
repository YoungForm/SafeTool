using SafeTool.Domain.Compliance;
using SafeTool.Domain.Standards;

namespace SafeTool.Application.Services;

/// <summary>
/// 合并报告服务（ISO/IEC双标准合并报告）
/// </summary>
public class CombinedReportService
{
    private readonly IReportGenerator _iso13849ReportGenerator;
    private readonly IIec62061ReportGenerator _iec62061ReportGenerator;
    private readonly IReportTemplateService _templateService;
    private readonly ILocalizationService _localizationService;

    public CombinedReportService(
        IReportGenerator iso13849ReportGenerator,
        IIec62061ReportGenerator iec62061ReportGenerator,
        IReportTemplateService templateService,
        ILocalizationService localizationService)
    {
        _iso13849ReportGenerator = iso13849ReportGenerator;
        _iec62061ReportGenerator = iec62061ReportGenerator;
        _templateService = templateService;
        _localizationService = localizationService;
    }

    /// <summary>
    /// 生成ISO/IEC双标准合并报告
    /// </summary>
    public async Task<string> GenerateCombinedReportAsync(
        ComplianceChecklist iso13849Checklist,
        EvaluationResult iso13849Result,
        SafetyFunction62061 iec62061Function,
        IEC62061EvaluationResult iec62061Result,
        string language = "zh-CN")
    {
        var iso13849Html = _iso13849ReportGenerator.GenerateHtml(iso13849Checklist, iso13849Result);
        var iec62061Html = _iec62061ReportGenerator.GenerateHtml(iec62061Function, iec62061Result);

        // 使用模板生成合并报告
        var template = await _templateService.GetTemplateAsync("default-compliance");
        if (template == null)
        {
            // 如果没有模板，使用默认合并格式
            return GenerateDefaultCombinedReport(iso13849Html, iec62061Html, language);
        }

        var combinedData = new
        {
            Title = _localizationService.GetString("CombinedReport", language),
            SystemName = iso13849Checklist.SystemName,
            Assessor = iso13849Checklist.Assessor,
            AssessmentDate = _localizationService.FormatDate(iso13849Checklist.AssessmentDate, language),
            ISO13849Content = ExtractBodyContent(iso13849Html),
            IEC62061Content = ExtractBodyContent(iec62061Html),
            GeneratedAt = _localizationService.FormatDateTime(DateTime.UtcNow, language)
        };

        return await _templateService.RenderAsync(template.Id, combinedData, language);
    }

    private string GenerateDefaultCombinedReport(string iso13849Html, string iec62061Html, string language)
    {
        var title = _localizationService.GetString("CombinedReport", language);
        var isoTitle = _localizationService.GetString("ISO13849Report", language);
        var iecTitle = _localizationService.GetString("IEC62061Report", language);

        return $@"<!doctype html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{title}</title>
    <style>
        body {{ font-family: Segoe UI, Arial; line-height: 1.6; padding: 24px; }}
        h1, h2 {{ margin: 0 0 8px; }}
        .section {{ margin: 24px 0; border-top: 2px solid #e5e7eb; padding-top: 16px; }}
    </style>
</head>
<body>
    <h1>{title}</h1>
    <div class='section'>
        <h2>{isoTitle}</h2>
        {ExtractBodyContent(iso13849Html)}
    </div>
    <div class='section'>
        <h2>{iecTitle}</h2>
        {ExtractBodyContent(iec62061Html)}
    </div>
    <div class='footer'>
        <p>{_localizationService.GetString("GeneratedAtLabel", language)}: {_localizationService.FormatDateTime(DateTime.UtcNow, language)}</p>
    </div>
</body>
</html>";
    }

    private string ExtractBodyContent(string html)
    {
        // 提取body标签内的内容
        var bodyMatch = System.Text.RegularExpressions.Regex.Match(html, @"<body[^>]*>(.*?)</body>", 
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return bodyMatch.Success ? bodyMatch.Groups[1].Value : html;
    }
}

