using System.Text.Json;
using System.Text.RegularExpressions;

namespace SafeTool.Application.Services;

/// <summary>
/// 数据导入增强服务（P2优先级）
/// 提供增强的数据导入功能，支持数据验证和转换
/// </summary>
public class DataImportEnhancementService
{
    private readonly ComponentLibraryService _componentLibrary;
    private readonly EvidenceService _evidenceService;

    public DataImportEnhancementService(
        ComponentLibraryService componentLibrary,
        EvidenceService evidenceService)
    {
        _componentLibrary = componentLibrary;
        _evidenceService = evidenceService;
    }

    /// <summary>
    /// 导入组件数据（增强版）
    /// </summary>
    public ImportResult<ComponentLibraryService.ComponentRecord> ImportComponents(
        string json,
        ImportOptions options)
    {
        var result = new ImportResult<ComponentLibraryService.ComponentRecord>
        {
            ImportedAt = DateTime.UtcNow,
            Options = options
        };

        try
        {
            var data = JsonSerializer.Deserialize<ComponentLibraryService.Library>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data == null || data.Items == null)
            {
                result.Success = false;
                result.Errors.Add("无效的JSON数据");
                return result;
            }

            var validated = new List<ComponentLibraryService.ComponentRecord>();
            var skipped = new List<string>();

            foreach (var component in data.Items)
            {
                var validation = ValidateComponent(component);
                if (validation.IsValid)
                {
                    if (options.OverwriteExisting || _componentLibrary.Get(component.Id) == null)
                    {
                        _componentLibrary.Add(component);
                        validated.Add(component);
                        result.ImportedCount++;
                    }
                    else
                    {
                        skipped.Add(component.Id);
                        result.SkippedCount++;
                    }
                }
                else
                {
                    result.Errors.AddRange(validation.Errors.Select(e => $"{component.Id}: {e}"));
                    result.FailedCount++;
                }
            }

            result.Success = result.Errors.Count == 0;
            result.ValidatedItems = validated;
            result.SkippedItems = skipped;

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"导入失败: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// 批量导入组件（从CSV）
    /// </summary>
    public ImportResult<ComponentLibraryService.ComponentRecord> ImportComponentsFromCsv(
        string csv,
        ImportOptions options)
    {
        var result = new ImportResult<ComponentLibraryService.ComponentRecord>
        {
            ImportedAt = DateTime.UtcNow,
            Options = options
        };

        try
        {
            var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
            {
                result.Success = false;
                result.Errors.Add("CSV数据至少需要包含标题行和一行数据");
                return result;
            }

            var headers = ParseCsvLine(lines[0]);
            var components = new List<ComponentLibraryService.ComponentRecord>();

            for (int i = 1; i < lines.Length; i++)
            {
                var values = ParseCsvLine(lines[i]);
                if (values.Length != headers.Length)
                {
                    result.Errors.Add($"第 {i + 1} 行列数不匹配");
                    continue;
                }

                var component = new ComponentLibraryService.ComponentRecord
                {
                    Id = GetValue(values, headers, "Id") ?? Guid.NewGuid().ToString("N"),
                    Manufacturer = GetValue(values, headers, "Manufacturer") ?? "",
                    Model = GetValue(values, headers, "Model") ?? "",
                    Category = GetValue(values, headers, "Category") ?? "",
                    Parameters = new Dictionary<string, string>()
                };

                // 解析参数
                var paramKeys = headers.Where(h => !new[] { "Id", "Manufacturer", "Model", "Category" }.Contains(h)).ToList();
                foreach (var key in paramKeys)
                {
                    var value = GetValue(values, headers, key);
                    if (!string.IsNullOrEmpty(value))
                    {
                        component.Parameters[key] = value;
                    }
                }

                var validation = ValidateComponent(component);
                if (validation.IsValid)
                {
                    components.Add(component);
                }
                else
                {
                    result.Errors.AddRange(validation.Errors.Select(e => $"{component.Id}: {e}"));
                }
            }

            // 导入验证通过的组件
            foreach (var component in components)
            {
                if (options.OverwriteExisting || _componentLibrary.Get(component.Id) == null)
                {
                    _componentLibrary.Add(component);
                    result.ImportedCount++;
                }
                else
                {
                    result.SkippedCount++;
                }
            }

            result.Success = result.Errors.Count == 0;
            result.ValidatedItems = components;

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"CSV导入失败: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// 验证组件
    /// </summary>
    private ValidationResult ValidateComponent(ComponentLibraryService.ComponentRecord component)
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(component.Id))
        {
            result.IsValid = false;
            result.Errors.Add("组件ID不能为空");
        }

        if (string.IsNullOrWhiteSpace(component.Manufacturer))
        {
            result.IsValid = false;
            result.Errors.Add("制造商不能为空");
        }

        if (string.IsNullOrWhiteSpace(component.Model))
        {
            result.IsValid = false;
            result.Errors.Add("型号不能为空");
        }

        if (string.IsNullOrWhiteSpace(component.Category))
        {
            result.IsValid = false;
            result.Errors.Add("类别不能为空");
        }

        return result;
    }

    /// <summary>
    /// 解析CSV行
    /// </summary>
    private string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString().Trim());
        return values.ToArray();
    }

    /// <summary>
    /// 获取CSV值
    /// </summary>
    private string? GetValue(string[] values, string[] headers, string headerName)
    {
        var index = Array.IndexOf(headers, headerName);
        return index >= 0 && index < values.Length ? values[index] : null;
    }
}

public class ImportOptions
{
    public bool OverwriteExisting { get; set; } = false;
    public bool ValidateBeforeImport { get; set; } = true;
    public bool StopOnError { get; set; } = false;
}

public class ImportResult<T>
{
    public DateTime ImportedAt { get; set; }
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<T> ValidatedItems { get; set; } = new();
    public List<string> SkippedItems { get; set; } = new();
    public ImportOptions? Options { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

