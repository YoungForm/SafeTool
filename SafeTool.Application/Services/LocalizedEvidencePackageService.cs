using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 本地化证据包导出服务
/// </summary>
public class LocalizedEvidencePackageService
{
    private readonly EvidenceService _evidenceService;
    private readonly ILocalizationService _localizationService;
    private readonly LocalizationEnhancementService _localizationEnhancement;

    public LocalizedEvidencePackageService(
        EvidenceService evidenceService,
        ILocalizationService localizationService,
        LocalizationEnhancementService localizationEnhancement)
    {
        _evidenceService = evidenceService;
        _localizationService = localizationService;
        _localizationEnhancement = localizationEnhancement;
    }

    /// <summary>
    /// 生成本地化证据包
    /// </summary>
    public LocalizedEvidencePackage GeneratePackage(
        string projectId,
        string language = "zh-CN",
        IEnumerable<string>? evidenceIds = null)
    {
        var package = new LocalizedEvidencePackage
        {
            ProjectId = projectId,
            Language = language,
            GeneratedAt = DateTime.UtcNow,
            EvidenceItems = new List<LocalizedEvidenceItem>(),
            Summary = new LocalizedEvidenceSummary()
        };

        // 获取证据列表
        var allEvidence = _evidenceService.List(null, null);
        var selectedEvidence = evidenceIds != null
            ? allEvidence.Where(e => evidenceIds.Contains(e.Id))
            : allEvidence;

        // 处理每个证据
        foreach (var evidence in selectedEvidence)
        {
            var fileName = !string.IsNullOrWhiteSpace(evidence.FilePath)
                ? Path.GetFileName(evidence.FilePath)
                : null;
            var fileSize = !string.IsNullOrWhiteSpace(evidence.FilePath) && File.Exists(evidence.FilePath)
                ? new FileInfo(evidence.FilePath).Length
                : 0;

            var localizedItem = new LocalizedEvidenceItem
            {
                Id = evidence.Id,
                Name = evidence.Name,
                Type = GetLocalizedType(evidence.Type, language),
                Source = evidence.Source ?? "",
                Issuer = evidence.Issuer ?? "",
                IssuedAt = evidence.CreatedAt != default
                    ? _localizationService.FormatDate(evidence.CreatedAt, language)
                    : null,
                ValidUntil = evidence.ValidUntil.HasValue
                    ? _localizationService.FormatDate(evidence.ValidUntil.Value, language)
                    : null,
                Status = GetLocalizedStatus(evidence.Status, language),
                Description = evidence.Note,
                FileName = fileName,
                FileSize = fileSize > 0 ? FormatFileSize(fileSize, language) : null
            };

            package.EvidenceItems.Add(localizedItem);
        }

        // 生成摘要
        package.Summary = GenerateSummary(selectedEvidence, language);

        return package;
    }

    /// <summary>
    /// 导出本地化证据包为JSON
    /// </summary>
    public string ExportToJson(LocalizedEvidencePackage package)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        return JsonSerializer.Serialize(package, options);
    }

    /// <summary>
    /// 生成证据包报告（HTML）
    /// </summary>
    public string GeneratePackageReport(LocalizedEvidencePackage package)
    {
        var language = package.Language;
        var title = _localizationService.GetString("EvidencePackage", language);
        var generatedAt = _localizationEnhancement.FormatDateTimeLocalized(
            package.GeneratedAt, "full", language);

        var evidenceRows = string.Join("", package.EvidenceItems.Select(e => $@"
            <tr>
                <td>{e.Name}</td>
                <td>{e.Type}</td>
                <td>{e.Source}</td>
                <td>{e.Issuer}</td>
                <td>{e.IssuedAt ?? "-"}</td>
                <td>{e.ValidUntil ?? "-"}</td>
                <td>{e.Status}</td>
            </tr>"));

        return $@"<!doctype html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{title}</title>
    <style>
        body {{ font-family: 'Microsoft YaHei', Arial, sans-serif; padding: 40px; }}
        h1 {{ text-align: center; margin-bottom: 40px; }}
        .summary {{ margin-bottom: 30px; padding: 20px; background-color: #f5f5f5; border-radius: 8px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
        th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
        th {{ background-color: #f2f2f2; font-weight: bold; }}
        .footer {{ margin-top: 40px; text-align: center; color: #666; }}
    </style>
</head>
<body>
    <h1>{title}</h1>
    <div class='summary'>
        <h2>{_localizationService.GetString("Summary", language)}</h2>
        <p><strong>{_localizationService.GetString("ProjectId", language)}:</strong> {package.ProjectId}</p>
        <p><strong>{_localizationService.GetString("TotalEvidence", language)}:</strong> {package.Summary.TotalCount}</p>
        <p><strong>{_localizationService.GetString("ValidEvidence", language)}:</strong> {package.Summary.ValidCount}</p>
        <p><strong>{_localizationService.GetString("ExpiredEvidence", language)}:</strong> {package.Summary.ExpiredCount}</p>
        <p><strong>{_localizationService.GetString("GeneratedAt", language)}:</strong> {generatedAt}</p>
    </div>
    <table>
        <thead>
            <tr>
                <th>{_localizationService.GetString("Name", language)}</th>
                <th>{_localizationService.GetString("Type", language)}</th>
                <th>{_localizationService.GetString("Source", language)}</th>
                <th>{_localizationService.GetString("Issuer", language)}</th>
                <th>{_localizationService.GetString("IssuedAt", language)}</th>
                <th>{_localizationService.GetString("ValidUntil", language)}</th>
                <th>{_localizationService.GetString("Status", language)}</th>
            </tr>
        </thead>
        <tbody>
            {evidenceRows}
        </tbody>
    </table>
    <div class='footer'>
        <p>{_localizationService.GetString("GeneratedBy", language)} SafeTool</p>
    </div>
</body>
</html>";
    }

    private string GetLocalizedType(string type, string language)
    {
        var typeMap = new Dictionary<string, Dictionary<string, string>>
        {
            ["Certificate"] = new Dictionary<string, string>
            {
                ["zh-CN"] = "证书",
                ["en-US"] = "Certificate"
            },
            ["TestReport"] = new Dictionary<string, string>
            {
                ["zh-CN"] = "测试报告",
                ["en-US"] = "Test Report"
            },
            ["DataSheet"] = new Dictionary<string, string>
            {
                ["zh-CN"] = "数据表",
                ["en-US"] = "Data Sheet"
            }
        };

        if (typeMap.TryGetValue(type, out var translations))
        {
            return translations.GetValueOrDefault(language, type);
        }

        return type;
    }

    private string GetLocalizedStatus(string status, string language)
    {
        var statusMap = new Dictionary<string, Dictionary<string, string>>
        {
            ["Valid"] = new Dictionary<string, string>
            {
                ["zh-CN"] = "有效",
                ["en-US"] = "Valid"
            },
            ["Expired"] = new Dictionary<string, string>
            {
                ["zh-CN"] = "已过期",
                ["en-US"] = "Expired"
            },
            ["Pending"] = new Dictionary<string, string>
            {
                ["zh-CN"] = "待审核",
                ["en-US"] = "Pending"
            }
        };

        if (statusMap.TryGetValue(status, out var translations))
        {
            return translations.GetValueOrDefault(language, status);
        }

        return status;
    }

    private string FormatFileSize(long bytes, string language)
    {
        string[] sizes = language.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? new[] { "B", "KB", "MB", "GB" }
            : new[] { "B", "KB", "MB", "GB" };

        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:F2} {sizes[order]}";
    }

    private LocalizedEvidenceSummary GenerateSummary(
        IEnumerable<EvidenceService.Evidence> evidence,
        string language)
    {
        var evidenceList = evidence.ToList();
        var now = DateTime.UtcNow;

        var totalSize = evidenceList
            .Where(e => !string.IsNullOrWhiteSpace(e.FilePath) && File.Exists(e.FilePath))
            .Sum(e => new FileInfo(e.FilePath!).Length);

        return new LocalizedEvidenceSummary
        {
            TotalCount = evidenceList.Count,
            ValidCount = evidenceList.Count(e => 
                e.ValidUntil == null || e.ValidUntil > now),
            ExpiredCount = evidenceList.Count(e => 
                e.ValidUntil.HasValue && e.ValidUntil <= now),
            TotalSize = totalSize
        };
    }
}

public class LocalizedEvidencePackage
{
    public string ProjectId { get; set; } = string.Empty;
    public string Language { get; set; } = "zh-CN";
    public DateTime GeneratedAt { get; set; }
    public List<LocalizedEvidenceItem> EvidenceItems { get; set; } = new();
    public LocalizedEvidenceSummary Summary { get; set; } = new();
}

public class LocalizedEvidenceItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string? IssuedAt { get; set; }
    public string? ValidUntil { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FileName { get; set; }
    public string? FileSize { get; set; }
}

public class LocalizedEvidenceSummary
{
    public int TotalCount { get; set; }
    public int ValidCount { get; set; }
    public int ExpiredCount { get; set; }
    public long TotalSize { get; set; }
}

