using SafeTool.Domain.Compliance;

namespace SafeTool.Application.Services;

/// <summary>
/// 批量报告生成服务（工厂模式）
/// </summary>
public class BatchReportService
{
    private readonly IReportGenerator _reportGenerator;
    private readonly IPdfReportService _pdfService;
    private readonly IReportTemplateService _templateService;
    private readonly ILocalizationService _localizationService;

    public BatchReportService(
        IReportGenerator reportGenerator,
        IPdfReportService pdfService,
        IReportTemplateService templateService,
        ILocalizationService localizationService)
    {
        _reportGenerator = reportGenerator;
        _pdfService = pdfService;
        _templateService = templateService;
        _localizationService = localizationService;
    }

    /// <summary>
    /// 批量生成报告（增强版）
    /// </summary>
    public async Task<BatchReportResult> GenerateBatchReportsAsync(
        IEnumerable<BatchReportRequest> requests,
        string format = "html",
        string language = "zh-CN",
        BatchReportOptions? options = null)
    {
        var opts = options ?? new BatchReportOptions();
        var result = new BatchReportResult
        {
            TotalCount = requests.Count(),
            GeneratedCount = 0,
            FailedCount = 0,
            Reports = new List<GeneratedReport>(),
            Errors = new List<string>(),
            StartedAt = DateTime.UtcNow
        };

        var requestList = requests.ToList();
        var tasks = new List<Task<GeneratedReport?>>();

        // 并行处理（如果启用）
        if (opts.EnableParallelProcessing && requestList.Count > 1)
        {
            foreach (var request in requestList)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        return await GenerateSingleReportAsync(request, format, language);
                    }
                    catch (Exception ex)
                    {
                        lock (result)
                        {
                            result.FailedCount++;
                            result.Errors.Add($"项目 {request.ProjectId} 报告生成失败: {ex.Message}");
                        }
                        return null;
                    }
                }));
            }

            var results = await Task.WhenAll(tasks);
            foreach (var report in results.Where(r => r != null))
            {
                result.Reports.Add(report!);
                result.GeneratedCount++;
            }
        }
        else
        {
            // 串行处理
            foreach (var request in requestList)
            {
                try
                {
                    var report = await GenerateSingleReportAsync(request, format, language);
                    result.Reports.Add(report);
                    result.GeneratedCount++;
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add($"项目 {request.ProjectId} 报告生成失败: {ex.Message}");
                }
            }
        }

        result.CompletedAt = DateTime.UtcNow;
        result.Duration = result.CompletedAt - result.StartedAt;

        return result;
    }

    private async Task<GeneratedReport> GenerateSingleReportAsync(
        BatchReportRequest request,
        string format,
        string language)
    {
        var report = new GeneratedReport
        {
            ProjectId = request.ProjectId,
            ReportType = request.ReportType,
            Format = format,
            Language = language,
            GeneratedAt = DateTime.UtcNow
        };

        switch (request.ReportType)
        {
            case "Compliance":
                if (request.ComplianceChecklist != null && request.EvaluationResult != null)
                {
                    if (format == "html")
                    {
                        report.Content = _reportGenerator.GenerateHtml(request.ComplianceChecklist, request.EvaluationResult);
                        report.ContentType = "text/html";
                    }
                    else if (format == "pdf")
                    {
                        var bytes = _pdfService.GenerateCompliancePdf(request.ComplianceChecklist, request.EvaluationResult);
                        report.Content = Convert.ToBase64String(bytes);
                        report.ContentType = "application/pdf";
                    }
                }
                break;

            case "SRS":
                // SRS报告生成
                break;

            case "IEC62061":
                // IEC 62061报告生成
                break;
        }

        return report;
    }

    /// <summary>
    /// 生成项目封面
    /// </summary>
    public string GenerateProjectCover(
        string projectId,
        string projectName,
        string? companyName = null,
        string? author = null,
        string language = "zh-CN")
    {
        var title = _localizationService.GetString("ComplianceReport", language);
        var company = companyName ?? "公司名称";
        var date = _localizationService.FormatDate(DateTime.UtcNow, language);

        return $@"<!doctype html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{title} - {projectName}</title>
    <style>
        body {{ font-family: 'Microsoft YaHei', Arial, sans-serif; margin: 0; padding: 0; }}
        .cover {{ width: 100%; height: 100vh; display: flex; flex-direction: column; justify-content: center; align-items: center; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; }}
        .cover-content {{ text-align: center; }}
        h1 {{ font-size: 48px; margin: 0 0 24px; font-weight: 300; }}
        h2 {{ font-size: 32px; margin: 0 0 48px; font-weight: 300; opacity: 0.9; }}
        .info {{ margin-top: 60px; font-size: 18px; opacity: 0.8; }}
        .info-item {{ margin: 12px 0; }}
    </style>
</head>
<body>
    <div class='cover'>
        <div class='cover-content'>
            <h1>{title}</h1>
            <h2>{projectName}</h2>
            <div class='info'>
                <div class='info-item'><strong>{_localizationService.GetString("CompanyLabel", language)}:</strong> {company}</div>
                <div class='info-item'><strong>{_localizationService.GetString("AuthorLabel", language)}:</strong> {author ?? "未指定"}</div>
                <div class='info-item'><strong>{_localizationService.GetString("DateLabel", language)}:</strong> {date}</div>
            </div>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// 生成签审页
    /// </summary>
    public string GenerateSignaturePage(
        IEnumerable<SignatureInfo> signatures,
        string language = "zh-CN")
    {
        var title = _localizationService.GetString("SignaturePage", language);
        var signerLabel = _localizationService.GetString("SignerLabel", language);
        var roleLabel = _localizationService.GetString("RoleLabel", language);
        var dateLabel = _localizationService.GetString("DateLabel", language);
        var commentLabel = _localizationService.GetString("CommentLabel", language);

        var signatureRows = string.Join("", signatures.Select(s => $@"
            <tr>
                <td>{s.Signer}</td>
                <td>{s.Role}</td>
                <td>{_localizationService.FormatDate(s.SignedAt, language)}</td>
                <td>{s.Comment ?? ""}</td>
            </tr>"));

        return $@"<!doctype html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{title}</title>
    <style>
        body {{ font-family: 'Microsoft YaHei', Arial, sans-serif; padding: 40px; }}
        h1 {{ text-align: center; margin-bottom: 40px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
        th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
        th {{ background-color: #f2f2f2; font-weight: bold; }}
    </style>
</head>
<body>
    <h1>{title}</h1>
    <table>
        <thead>
            <tr>
                <th>{signerLabel}</th>
                <th>{roleLabel}</th>
                <th>{dateLabel}</th>
                <th>{commentLabel}</th>
            </tr>
        </thead>
        <tbody>
            {signatureRows}
        </tbody>
    </table>
</body>
</html>";
    }
}

public class BatchReportRequest
{
    public string ProjectId { get; set; } = string.Empty;
    public string ReportType { get; set; } = "Compliance"; // Compliance/SRS/IEC62061
    public ComplianceChecklist? ComplianceChecklist { get; set; }
    public EvaluationResult? EvaluationResult { get; set; }
}

public class BatchReportResult
{
    public int TotalCount { get; set; }
    public int GeneratedCount { get; set; }
    public int FailedCount { get; set; }
    public List<GeneratedReport> Reports { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
}

public class BatchReportOptions
{
    public bool EnableParallelProcessing { get; set; } = true;
    public int MaxConcurrency { get; set; } = 5;
    public bool IncludeCoverPage { get; set; } = true;
    public bool IncludeSignaturePage { get; set; } = true;
}

public class GeneratedReport
{
    public string ProjectId { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? ContentType { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class SignatureInfo
{
    public string Signer { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    public string? Comment { get; set; }
}

