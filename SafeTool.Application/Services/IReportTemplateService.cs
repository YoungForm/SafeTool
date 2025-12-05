namespace SafeTool.Application.Services;

/// <summary>
/// 报告模板服务接口（模板方法模式）
/// </summary>
public interface IReportTemplateService
{
    Task<ReportTemplate?> GetTemplateAsync(string templateId);
    Task<IEnumerable<ReportTemplate>> ListTemplatesAsync();
    Task<ReportTemplate> CreateTemplateAsync(ReportTemplate template);
    Task<ReportTemplate> UpdateTemplateAsync(string templateId, ReportTemplate template);
    Task<bool> DeleteTemplateAsync(string templateId);
    Task<string> RenderAsync(string templateId, object data, string language = "zh-CN");
}

/// <summary>
/// 报告模板模型
/// </summary>
public class ReportTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ReportTemplateType Type { get; set; }
    public string TemplateContent { get; set; } = string.Empty; // HTML模板内容，支持占位符
    public Dictionary<string, string> Placeholders { get; set; } = new(); // 占位符说明
    public bool IsDefault { get; set; }
    public string Language { get; set; } = "zh-CN";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum ReportTemplateType
{
    ComplianceReport,    // 合规报告
    SRSDocument,         // SRS文档
    IEC62061Report,      // IEC 62061报告
    CombinedReport       // 合并报告
}

