using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SafeTool.Application.Services;

/// <summary>
/// 互通格式服务（适配器模式）
/// </summary>
public class InteropFormatService
{
    private readonly ComponentLibraryService _componentLibrary;
    private readonly ProjectModelService _projectModel;

    public InteropFormatService(ComponentLibraryService componentLibrary, ProjectModelService projectModel)
    {
        _componentLibrary = componentLibrary;
        _projectModel = projectModel;
    }

    /// <summary>
    /// 导出为SISTEMA兼容格式（CSV）
    /// </summary>
    public string ExportToSistemaCsv(string projectId)
    {
        var sb = new System.Text.StringBuilder();
        
        // SISTEMA CSV格式：ID,Manufacturer,Model,Category,MTTFd,DCavg,PFHd,Beta
        sb.AppendLine("ID,Manufacturer,Model,Category,MTTFd,DCavg,PFHd,Beta");
        
        var components = _componentLibrary.List();
        foreach (var comp in components)
        {
            var mttfd = comp.Parameters?.GetValueOrDefault("MTTFd") ?? comp.Parameters?.GetValueOrDefault("mttfd") ?? "";
            var dcavg = comp.Parameters?.GetValueOrDefault("DCavg") ?? comp.Parameters?.GetValueOrDefault("DCcapability") ?? "";
            var pfhd = comp.Parameters?.GetValueOrDefault("PFHd") ?? comp.Parameters?.GetValueOrDefault("pfhd") ?? "";
            var beta = comp.Parameters?.GetValueOrDefault("beta") ?? comp.Parameters?.GetValueOrDefault("Beta") ?? "";
            
            sb.AppendLine($"{comp.Id},{comp.Manufacturer},{comp.Model},{comp.Category},{mttfd},{dcavg},{pfhd},{beta}");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// 导入SISTEMA CSV格式
    /// </summary>
    public int ImportFromSistemaCsv(string csv)
    {
        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return 0;
        
        int count = 0;
        var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
        
        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length < headers.Length) continue;
            
            var comp = new ComponentLibraryService.ComponentRecord
            {
                Id = GetValue(values, headers, "ID") ?? Guid.NewGuid().ToString("N"),
                Manufacturer = GetValue(values, headers, "Manufacturer") ?? "",
                Model = GetValue(values, headers, "Model") ?? "",
                Category = GetValue(values, headers, "Category") ?? "sensor",
                Parameters = new Dictionary<string, string>()
            };
            
            var mttfd = GetValue(values, headers, "MTTFd");
            if (!string.IsNullOrWhiteSpace(mttfd))
                comp.Parameters["MTTFd"] = mttfd;
            
            var dcavg = GetValue(values, headers, "DCavg");
            if (!string.IsNullOrWhiteSpace(dcavg))
                comp.Parameters["DCavg"] = dcavg;
            
            var pfhd = GetValue(values, headers, "PFHd");
            if (!string.IsNullOrWhiteSpace(pfhd))
                comp.Parameters["PFHd"] = pfhd;
            
            var beta = GetValue(values, headers, "Beta");
            if (!string.IsNullOrWhiteSpace(beta))
                comp.Parameters["beta"] = beta;
            
            _componentLibrary.Add(comp);
            count++;
        }
        
        return count;
    }

    /// <summary>
    /// 导出为PAScal兼容格式（JSON）
    /// </summary>
    public string ExportToPascalJson(string projectId)
    {
        var functions = _projectModel.List().ToList();
        var components = _componentLibrary.List().ToList();
        
        var pascalFormat = new
        {
            version = "1.0",
            project = projectId,
            exportedAt = DateTime.UtcNow,
            functions = functions.Select(f => new
            {
                id = f.Id,
                name = f.Name,
                standard = f.Standard,
                target = f.Target,
                channels = new
                {
                    input = f.Model.I?.Count ?? 0,
                    logic = f.Model.L?.Count ?? 0,
                    output = f.Model.O?.Count ?? 0
                }
            }),
            components = components.Select(c => new
            {
                id = c.Id,
                manufacturer = c.Manufacturer,
                model = c.Model,
                category = c.Category,
                parameters = c.Parameters
            })
        };
        
        return JsonSerializer.Serialize(pascalFormat, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// 导入PAScal JSON格式
    /// </summary>
    public int ImportFromPascalJson(string json)
    {
        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            int count = 0;
            
            // 导入组件
            if (data.TryGetProperty("components", out var components))
            {
                foreach (var comp in components.EnumerateArray())
                {
                    var component = new ComponentLibraryService.ComponentRecord
                    {
                        Id = comp.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N"),
                        Manufacturer = comp.TryGetProperty("manufacturer", out var m) ? m.GetString() ?? "" : "",
                        Model = comp.TryGetProperty("model", out var mdl) ? mdl.GetString() ?? "" : "",
                        Category = comp.TryGetProperty("category", out var cat) ? cat.GetString() ?? "sensor" : "sensor",
                        Parameters = new Dictionary<string, string>()
                    };
                    
                    if (comp.TryGetProperty("parameters", out var paramsObj))
                    {
                        foreach (var prop in paramsObj.EnumerateObject())
                        {
                            component.Parameters[prop.Name] = prop.Value.GetString() ?? "";
                        }
                    }
                    
                    _componentLibrary.Add(component);
                    count++;
                }
            }
            
            return count;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 导出为Siemens SET兼容格式（JSON）
    /// </summary>
    public string ExportToSiemensSetJson(string projectId)
    {
        var functions = _projectModel.List().ToList();
        
        var setFormat = new
        {
            tool = "Siemens SET",
            version = "1.0",
            project = projectId,
            exportedAt = DateTime.UtcNow,
            safetyFunctions = functions.Select(f => new
            {
                functionId = f.Id,
                functionName = f.Name,
                standard = f.Standard,
                targetLevel = f.Target,
                subsystems = f.Model.L?.Select((l, idx) => new
                {
                    subsystemId = $"SUB-{idx + 1}",
                    components = new[] { l.Id }
                }).ToArray() ?? Array.Empty<object>()
            })
        };
        
        return JsonSerializer.Serialize(setFormat, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// 获取中间格式规范文档
    /// </summary>
    public InteropFormatSpecification GetFormatSpecification()
    {
        return new InteropFormatSpecification
        {
            Version = "1.0",
            SupportedFormats = new List<FormatInfo>
            {
                new FormatInfo
                {
                    FormatName = "SISTEMA CSV",
                    Description = "SISTEMA兼容的CSV格式",
                    Fields = new List<string> { "ID", "Manufacturer", "Model", "Category", "MTTFd", "DCavg", "PFHd", "Beta" },
                    Example = "comp-001,Contoso,SafeSensor-200,sensor,10000000,0.9,1e-7,0.05"
                },
                new FormatInfo
                {
                    FormatName = "PAScal JSON",
                    Description = "PAScal兼容的JSON格式",
                    Fields = new List<string> { "version", "project", "functions", "components" },
                    Example = "{\"version\":\"1.0\",\"project\":\"demo\",\"functions\":[],\"components\":[]}"
                },
                new FormatInfo
                {
                    FormatName = "Siemens SET JSON",
                    Description = "Siemens SET兼容的JSON格式",
                    Fields = new List<string> { "tool", "version", "project", "safetyFunctions" },
                    Example = "{\"tool\":\"Siemens SET\",\"version\":\"1.0\",\"project\":\"demo\",\"safetyFunctions\":[]}"
                },
                new FormatInfo
                {
                    FormatName = "SafeTool Native JSON",
                    Description = "SafeTool原生JSON格式",
                    Fields = new List<string> { "meta", "functions", "components", "evidence" },
                    Example = "{\"meta\":{},\"functions\":[],\"components\":[],\"evidence\":[]}"
                }
            },
            MappingRules = new Dictionary<string, string>
            {
                { "MTTFd", "平均危险失效时间（小时）" },
                { "DCavg", "平均诊断覆盖率" },
                { "PFHd", "每小时危险失效概率" },
                { "Beta", "共因失效因子" },
                { "B10d", "10%危险失效时的操作次数" }
            }
        };
    }

    private string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        
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

    private string? GetValue(string[] values, string[] headers, string headerName)
    {
        var index = Array.IndexOf(headers, headerName);
        return index >= 0 && index < values.Length ? values[index] : null;
    }
}

public class InteropFormatSpecification
{
    public string Version { get; set; } = string.Empty;
    public List<FormatInfo> SupportedFormats { get; set; } = new();
    public Dictionary<string, string> MappingRules { get; set; } = new();
}

public class FormatInfo
{
    public string FormatName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Fields { get; set; } = new();
    public string Example { get; set; } = string.Empty;
}

