using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 项目封面与签审页服务（P1优先级）
/// 生成项目报告封面和签审页
/// </summary>
public class ProjectCoverPageService
{
    private readonly ILocalizationService _localizationService;
    private readonly string _dataDir;

    public ProjectCoverPageService(ILocalizationService localizationService, string dataDir)
    {
        _localizationService = localizationService;
        _dataDir = dataDir;
        EnsureDirectories();
    }

    /// <summary>
    /// 生成项目封面
    /// </summary>
    public CoverPageResult GenerateCoverPage(CoverPageRequest request)
    {
        var coverPage = new CoverPageInfo
        {
            ProjectId = request.ProjectId,
            ProjectName = request.ProjectName,
            ReportTitle = request.ReportTitle ?? _localizationService.GetString("ComplianceReport", request.Language),
            CompanyName = request.CompanyName,
            Department = request.Department,
            Author = request.Author,
            Reviewer = request.Reviewer,
            Date = request.Date ?? DateTime.UtcNow,
            Version = request.Version ?? "1.0",
            Language = request.Language,
            GeneratedAt = DateTime.UtcNow
        };

        var html = GenerateCoverPageHtml(coverPage);
        var pdf = GenerateCoverPagePdf(coverPage);

        SaveCoverPage(coverPage);

        return new CoverPageResult
        {
            CoverPage = coverPage,
            HtmlContent = html,
            PdfContent = pdf
        };
    }

    /// <summary>
    /// 生成签审页
    /// </summary>
    public SignaturePageResult GenerateSignaturePage(SignaturePageRequest request)
    {
        var signaturePage = new SignaturePageInfo
        {
            ProjectId = request.ProjectId,
            Signatures = request.Signatures ?? new List<SignatureEntry>(),
            Language = request.Language,
            GeneratedAt = DateTime.UtcNow
        };

        var html = GenerateSignaturePageHtml(signaturePage);
        var pdf = GenerateSignaturePagePdf(signaturePage);

        SaveSignaturePage(signaturePage);

        return new SignaturePageResult
        {
            SignaturePage = signaturePage,
            HtmlContent = html,
            PdfContent = pdf
        };
    }

    /// <summary>
    /// 生成完整报告（包含封面和签审页）
    /// </summary>
    public CompleteReportResult GenerateCompleteReport(CompleteReportRequest request)
    {
        var result = new CompleteReportResult
        {
            ProjectId = request.ProjectId,
            GeneratedAt = DateTime.UtcNow
        };

        // 生成封面
        if (request.IncludeCoverPage)
        {
            var coverRequest = new CoverPageRequest
            {
                ProjectId = request.ProjectId,
                ProjectName = request.ProjectName,
                ReportTitle = request.ReportTitle,
                CompanyName = request.CompanyName,
                Author = request.Author,
                Language = request.Language
            };
            result.CoverPage = GenerateCoverPage(coverRequest);
        }

        // 生成签审页
        if (request.IncludeSignaturePage && request.Signatures != null)
        {
            var signatureRequest = new SignaturePageRequest
            {
                ProjectId = request.ProjectId,
                Signatures = request.Signatures,
                Language = request.Language
            };
            result.SignaturePage = GenerateSignaturePage(signatureRequest);
        }

        // 合并内容
        result.CombinedHtml = CombineReportContent(result);

        return result;
    }

    /// <summary>
    /// 生成封面HTML
    /// </summary>
    private string GenerateCoverPageHtml(CoverPageInfo coverPage)
    {
        var title = _localizationService.GetString("ComplianceReport", coverPage.Language);
        var companyLabel = _localizationService.GetString("CompanyLabel", coverPage.Language);
        var authorLabel = _localizationService.GetString("AuthorLabel", coverPage.Language);
        var dateLabel = _localizationService.GetString("DateLabel", coverPage.Language);
        var versionLabel = _localizationService.GetString("VersionLabel", coverPage.Language);
        var date = _localizationService.FormatDate(coverPage.Date, coverPage.Language);

        return $@"<!doctype html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{title} - {coverPage.ProjectName}</title>
    <style>
        body {{ font-family: 'Microsoft YaHei', Arial, sans-serif; margin: 0; padding: 0; }}
        .cover {{ width: 100%; height: 100vh; display: flex; flex-direction: column; justify-content: center; align-items: center; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; page-break-after: always; }}
        .cover-content {{ text-align: center; max-width: 800px; padding: 40px; }}
        h1 {{ font-size: 48px; margin: 0 0 24px; font-weight: 300; }}
        h2 {{ font-size: 32px; margin: 0 0 48px; font-weight: 300; opacity: 0.9; }}
        .info {{ margin-top: 60px; font-size: 18px; opacity: 0.8; }}
        .info-item {{ margin: 12px 0; }}
        .version {{ position: absolute; bottom: 20px; right: 20px; font-size: 14px; opacity: 0.7; }}
    </style>
</head>
<body>
    <div class='cover'>
        <div class='cover-content'>
            <h1>{coverPage.ReportTitle}</h1>
            <h2>{coverPage.ProjectName}</h2>
            <div class='info'>
                <div class='info-item'><strong>{companyLabel}:</strong> {coverPage.CompanyName ?? "未指定"}</div>
                <div class='info-item'><strong>{authorLabel}:</strong> {coverPage.Author ?? "未指定"}</div>
                <div class='info-item'><strong>{dateLabel}:</strong> {date}</div>
                {(!string.IsNullOrEmpty(coverPage.Department) ? $"<div class='info-item'><strong>部门:</strong> {coverPage.Department}</div>" : "")}
            </div>
            <div class='version'>{versionLabel}: {coverPage.Version}</div>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// 生成签审页HTML
    /// </summary>
    private string GenerateSignaturePageHtml(SignaturePageInfo signaturePage)
    {
        var title = _localizationService.GetString("SignaturePage", signaturePage.Language);
        var signerLabel = _localizationService.GetString("SignerLabel", signaturePage.Language);
        var roleLabel = _localizationService.GetString("RoleLabel", signaturePage.Language);
        var dateLabel = _localizationService.GetString("DateLabel", signaturePage.Language);
        var commentLabel = _localizationService.GetString("CommentLabel", signaturePage.Language);

        var signatureRows = string.Join("", signaturePage.Signatures.Select(s => $@"
            <tr>
                <td>{s.Signer}</td>
                <td>{s.Role}</td>
                <td>{_localizationService.FormatDate(s.SignedAt, signaturePage.Language)}</td>
                <td>{s.Comment ?? ""}</td>
            </tr>"));

        return $@"<!doctype html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{title}</title>
    <style>
        body {{ font-family: 'Microsoft YaHei', Arial, sans-serif; padding: 40px; page-break-after: always; }}
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

    /// <summary>
    /// 生成封面PDF（简化实现，实际应使用PDF库）
    /// </summary>
    private byte[] GenerateCoverPagePdf(CoverPageInfo coverPage)
    {
        // 简化实现，返回空字节数组
        // 实际应使用QuestPDF或其他PDF库生成
        return Array.Empty<byte>();
    }

    /// <summary>
    /// 生成签审页PDF（简化实现，实际应使用PDF库）
    /// </summary>
    private byte[] GenerateSignaturePagePdf(SignaturePageInfo signaturePage)
    {
        // 简化实现，返回空字节数组
        // 实际应使用QuestPDF或其他PDF库生成
        return Array.Empty<byte>();
    }

    /// <summary>
    /// 合并报告内容
    /// </summary>
    private string CombineReportContent(CompleteReportResult result)
    {
        var parts = new List<string>();

        if (result.CoverPage != null)
        {
            parts.Add(result.CoverPage.HtmlContent);
        }

        // 这里可以添加报告主体内容

        if (result.SignaturePage != null)
        {
            parts.Add(result.SignaturePage.HtmlContent);
        }

        return string.Join("\n", parts);
    }

    /// <summary>
    /// 保存封面页
    /// </summary>
    private void SaveCoverPage(CoverPageInfo coverPage)
    {
        var path = Path.Combine(_dataDir, "reports", coverPage.ProjectId, "cover-page.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(coverPage, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 保存签审页
    /// </summary>
    private void SaveSignaturePage(SignaturePageInfo signaturePage)
    {
        var path = Path.Combine(_dataDir, "reports", signaturePage.ProjectId, "signature-page.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(signaturePage, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private void EnsureDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_dataDir, "reports"));
    }
}

public class CoverPageRequest
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? ReportTitle { get; set; }
    public string? CompanyName { get; set; }
    public string? Department { get; set; }
    public string? Author { get; set; }
    public string? Reviewer { get; set; }
    public DateTime? Date { get; set; }
    public string? Version { get; set; }
    public string Language { get; set; } = "zh-CN";
}

public class SignaturePageRequest
{
    public string ProjectId { get; set; } = string.Empty;
    public List<SignatureEntry>? Signatures { get; set; }
    public string Language { get; set; } = "zh-CN";
}

public class CompleteReportRequest
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? ReportTitle { get; set; }
    public string? CompanyName { get; set; }
    public string? Author { get; set; }
    public bool IncludeCoverPage { get; set; } = true;
    public bool IncludeSignaturePage { get; set; } = true;
    public List<SignatureEntry>? Signatures { get; set; }
    public string Language { get; set; } = "zh-CN";
}

public class CoverPageInfo
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ReportTitle { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Department { get; set; }
    public string? Author { get; set; }
    public string? Reviewer { get; set; }
    public DateTime Date { get; set; }
    public string Version { get; set; } = "1.0";
    public string Language { get; set; } = "zh-CN";
    public DateTime GeneratedAt { get; set; }
}

public class SignaturePageInfo
{
    public string ProjectId { get; set; } = string.Empty;
    public List<SignatureEntry> Signatures { get; set; } = new();
    public string Language { get; set; } = "zh-CN";
    public DateTime GeneratedAt { get; set; }
}

public class SignatureEntry
{
    public string Signer { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    public string? Comment { get; set; }
}

public class CoverPageResult
{
    public CoverPageInfo CoverPage { get; set; } = new();
    public string HtmlContent { get; set; } = string.Empty;
    public byte[] PdfContent { get; set; } = Array.Empty<byte>();
}

public class SignaturePageResult
{
    public SignaturePageInfo SignaturePage { get; set; } = new();
    public string HtmlContent { get; set; } = string.Empty;
    public byte[] PdfContent { get; set; } = Array.Empty<byte>();
}

public class CompleteReportResult
{
    public string ProjectId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public CoverPageResult? CoverPage { get; set; }
    public SignaturePageResult? SignaturePage { get; set; }
    public string CombinedHtml { get; set; } = string.Empty;
}

