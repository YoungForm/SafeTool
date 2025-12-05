using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 数据导出增强服务（P2优先级）
/// 提供增强的数据导出功能
/// </summary>
public class DataExportEnhancementService
{
    private readonly ComponentLibraryService _componentLibrary;
    private readonly EvidenceService _evidenceService;
    private readonly VerificationChecklistService _checklistService;
    private readonly ComplianceMatrixService _matrixService;

    public DataExportEnhancementService(
        ComponentLibraryService componentLibrary,
        EvidenceService evidenceService,
        VerificationChecklistService checklistService,
        ComplianceMatrixService matrixService)
    {
        _componentLibrary = componentLibrary;
        _evidenceService = evidenceService;
        _checklistService = checklistService;
        _matrixService = matrixService;
    }

    /// <summary>
    /// 导出完整项目数据
    /// </summary>
    public ProjectExportResult ExportProjectData(string projectId, ExportOptions options)
    {
        var result = new ProjectExportResult
        {
            ProjectId = projectId,
            ExportedAt = DateTime.UtcNow,
            Format = options.Format,
            Files = new List<ExportFile>()
        };

        // 导出组件库
        if (options.IncludeComponents)
        {
            var components = _componentLibrary.List().ToList();
            var componentData = ExportComponents(components, options.Format);
            result.Files.Add(new ExportFile
            {
                Type = "Components",
                FileName = $"components.{GetFileExtension(options.Format)}",
                Content = componentData,
                Size = componentData.Length
            });
        }

        // 导出证据
        if (options.IncludeEvidence)
        {
            var evidence = _evidenceService.List(null, null).ToList();
            var evidenceData = ExportEvidence(evidence, options.Format);
            result.Files.Add(new ExportFile
            {
                Type = "Evidence",
                FileName = $"evidence.{GetFileExtension(options.Format)}",
                Content = evidenceData,
                Size = evidenceData.Length
            });
        }

        // 导出检查清单
        if (options.IncludeChecklists)
        {
            var checklists = _checklistService.List(projectId).ToList();
            var checklistData = ExportChecklists(checklists, options.Format);
            result.Files.Add(new ExportFile
            {
                Type = "Checklists",
                FileName = $"checklists.{GetFileExtension(options.Format)}",
                Content = checklistData,
                Size = checklistData.Length
            });
        }

        // 导出合规矩阵
        if (options.IncludeMatrices)
        {
            var entries = _matrixService.Get(projectId).ToList();
            var matrixData = ExportMatrices(entries, options.Format);
            result.Files.Add(new ExportFile
            {
                Type = "Matrices",
                FileName = $"matrices.{GetFileExtension(options.Format)}",
                Content = matrixData,
                Size = matrixData.Length
            });
        }

        result.TotalSize = result.Files.Sum(f => f.Size);
        result.FileCount = result.Files.Count;

        return result;
    }

    /// <summary>
    /// 导出组件
    /// </summary>
    private string ExportComponents(List<ComponentLibraryService.ComponentRecord> components, ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Json => JsonSerializer.Serialize(components, new JsonSerializerOptions { WriteIndented = true }),
            ExportFormat.Csv => ExportComponentsToCsv(components),
            ExportFormat.Excel => ExportComponentsToCsv(components), // 简化处理，实际应使用Excel库
            _ => JsonSerializer.Serialize(components, new JsonSerializerOptions { WriteIndented = true })
        };
    }

    /// <summary>
    /// 导出组件到CSV
    /// </summary>
    private string ExportComponentsToCsv(List<ComponentLibraryService.ComponentRecord> components)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Id,Manufacturer,Model,Category,Parameters");

        foreach (var component in components)
        {
            var parameters = JsonSerializer.Serialize(component.Parameters);
            sb.AppendLine($"{component.Id},{component.Manufacturer},{component.Model},{component.Category},\"{parameters}\"");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 导出证据
    /// </summary>
    private string ExportEvidence(List<EvidenceService.Evidence> evidence, ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Json => JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }),
            ExportFormat.Csv => ExportEvidenceToCsv(evidence),
            _ => JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true })
        };
    }

    /// <summary>
    /// 导出证据到CSV
    /// </summary>
    private string ExportEvidenceToCsv(List<EvidenceService.Evidence> evidence)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Id,Name,Type,Source,Issuer,IssuedAt,ValidUntil,FilePath");

        foreach (var ev in evidence)
        {
            sb.AppendLine($"{ev.Id},{ev.Name},{ev.Type},{ev.Source},{ev.Issuer},{ev.IssuedAt:yyyy-MM-dd},{ev.ValidUntil:yyyy-MM-dd},{ev.FilePath}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 导出检查清单
    /// </summary>
    private string ExportChecklists(List<SafeTool.Domain.Compliance.ComplianceChecklist> checklists, ExportFormat format)
    {
        return JsonSerializer.Serialize(checklists, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// 导出合规矩阵
    /// </summary>
    private string ExportMatrices(List<ComplianceMatrixService.Entry> entries, ExportFormat format)
    {
        if (format == ExportFormat.Csv)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("标准,条款,要求摘要,引用,证据ID,结果,责任人,期限");
            foreach (var entry in entries)
            {
                sb.AppendLine($"{entry.Standard},{entry.Clause},{entry.Requirement},{entry.Reference},{entry.EvidenceId ?? ""},{entry.Result},{entry.Owner ?? ""},{entry.Due ?? ""}");
            }
            return sb.ToString();
        }
        return JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// 获取文件扩展名
    /// </summary>
    private string GetFileExtension(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Json => "json",
            ExportFormat.Csv => "csv",
            ExportFormat.Excel => "xlsx",
            _ => "json"
        };
    }
}

public enum ExportFormat
{
    Json,
    Csv,
    Excel
}

public class ExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.Json;
    public bool IncludeComponents { get; set; } = true;
    public bool IncludeEvidence { get; set; } = true;
    public bool IncludeChecklists { get; set; } = true;
    public bool IncludeMatrices { get; set; } = true;
}

public class ProjectExportResult
{
    public string ProjectId { get; set; } = string.Empty;
    public DateTime ExportedAt { get; set; }
    public ExportFormat Format { get; set; }
    public int FileCount { get; set; }
    public long TotalSize { get; set; }
    public List<ExportFile> Files { get; set; } = new();
}

public class ExportFile
{
    public string Type { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long Size { get; set; }
}

