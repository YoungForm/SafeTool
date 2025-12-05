using System.Text;
using System.Text.RegularExpressions;

namespace SafeTool.Application.Services;

/// <summary>
/// 报告模板服务实现（模板方法模式 + 策略模式）
/// </summary>
public class ReportTemplateService : IReportTemplateService
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private Dictionary<string, ReportTemplate> _templates = new();
    private readonly ILocalizationService _localizationService;

    public ReportTemplateService(string dataDir, ILocalizationService localizationService)
    {
        var dir = Path.Combine(dataDir, "ReportTemplates");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "templates.json");
        _localizationService = localizationService;
        Load();
        InitializeDefaultTemplates();
    }

    private void Load()
    {
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ReportTemplate>>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data != null)
                _templates = data;
        }
    }

    private void Save()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_templates,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    private void InitializeDefaultTemplates()
    {
        if (_templates.Count > 0) return;

        // 默认合规报告模板
        var defaultTemplate = new ReportTemplate
        {
            Id = "default-compliance",
            Name = "默认合规报告模板",
            Description = "标准合规自检报告模板",
            Type = ReportTemplateType.ComplianceReport,
            TemplateContent = GetDefaultComplianceTemplate(),
            IsDefault = true,
            Language = "zh-CN"
        };
        _templates[defaultTemplate.Id] = defaultTemplate;
        Save();
    }

    private string GetDefaultComplianceTemplate()
    {
        return @"<!doctype html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{{Title}}</title>
    <style>{{Styles}}</style>
</head>
<body>
    <h1>{{Title}}</h1>
    <div class='header'>
        <p><strong>{{SystemLabel}}:</strong> {{SystemName}}</p>
        <p><strong>{{AssessorLabel}}:</strong> {{Assessor}}</p>
        <p><strong>{{DateLabel}}:</strong> {{AssessmentDate}}</p>
    </div>
    <div class='summary'>
        <p><strong>{{ConclusionLabel}}:</strong> <span class='{{ConclusionClass}}'>{{Summary}}</span></p>
    </div>
    {{Content}}
    <div class='footer'>
        <p>{{GeneratedAtLabel}}: {{GeneratedAt}}</p>
    </div>
</body>
</html>";
    }

    public Task<ReportTemplate?> GetTemplateAsync(string templateId)
    {
        lock (_lock)
        {
            return Task.FromResult(_templates.TryGetValue(templateId, out var template) ? template : null);
        }
    }

    public Task<IEnumerable<ReportTemplate>> ListTemplatesAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_templates.Values.AsEnumerable());
        }
    }

    public Task<ReportTemplate> CreateTemplateAsync(ReportTemplate template)
    {
        lock (_lock)
        {
            template.Id = template.Id ?? Guid.NewGuid().ToString("N");
            template.CreatedAt = DateTime.UtcNow;
            _templates[template.Id] = template;
            Save();
            return Task.FromResult(template);
        }
    }

    public Task<ReportTemplate> UpdateTemplateAsync(string templateId, ReportTemplate template)
    {
        lock (_lock)
        {
            if (!_templates.ContainsKey(templateId))
                throw new KeyNotFoundException($"模板 {templateId} 不存在");

            template.Id = templateId;
            template.UpdatedAt = DateTime.UtcNow;
            _templates[templateId] = template;
            Save();
            return Task.FromResult(template);
        }
    }

    public Task<bool> DeleteTemplateAsync(string templateId)
    {
        lock (_lock)
        {
            var removed = _templates.Remove(templateId);
            if (removed)
                Save();
            return Task.FromResult(removed);
        }
    }

    public Task<string> RenderAsync(string templateId, object data, string language = "zh-CN")
    {
        var template = GetTemplateAsync(templateId).Result;
        if (template == null)
            throw new KeyNotFoundException($"模板 {templateId} 不存在");

        var content = template.TemplateContent;
        var localized = _localizationService.Localize(content, language);

        // 使用反射获取数据对象的属性值
        var dataType = data.GetType();
        var properties = dataType.GetProperties();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(data);
            var placeholder = $"{{{{{prop.Name}}}}}";
            var stringValue = value?.ToString() ?? "";
            localized = localized.Replace(placeholder, stringValue);
        }

        // 替换本地化标签
        localized = ReplaceLocalizedPlaceholders(localized, language);

        return Task.FromResult(localized);
    }

    private string ReplaceLocalizedPlaceholders(string content, string language)
    {
        // 替换常见的本地化占位符
        var localizations = _localizationService.GetLocalizations(language);
        foreach (var kvp in localizations)
        {
            content = content.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }
        return content;
    }
}

