using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SafeTool.Application.Services;

/// <summary>
/// SISTEMA格式解析器（.slib/.spp文件解析）
/// </summary>
public class SistemaFormatParser
{
    /// <summary>
    /// 解析SISTEMA库文件（.slib）
    /// 注意：.slib是二进制格式，这里提供基础解析框架
    /// </summary>
    public SistemaLibraryResult ParseSistemaLibrary(byte[] fileData, string? fileName = null)
    {
        var result = new SistemaLibraryResult
        {
            FileName = fileName ?? "unknown.slib",
            Components = new List<SistemaComponent>(),
            Warnings = new List<string>()
        };

        try
        {
            // 尝试作为文本格式解析（某些SISTEMA版本可能使用文本格式）
            var text = Encoding.UTF8.GetString(fileData);
            
            // 检查是否是XML格式
            if (text.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                return ParseSistemaXml(text, result);
            }

            // 检查是否是JSON格式
            if (text.TrimStart().StartsWith("{") || text.TrimStart().StartsWith("["))
            {
                return ParseSistemaJson(text, result);
            }

            // 尝试作为二进制格式解析
            return ParseSistemaBinary(fileData, result);
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"解析失败: {ex.Message}");
            result.Warnings.Add("尝试使用CSV格式导入");
            return result;
        }
    }

    /// <summary>
    /// 解析SISTEMA项目文件（.spp）
    /// </summary>
    public SistemaProjectResult ParseSistemaProject(byte[] fileData, string? fileName = null)
    {
        var result = new SistemaProjectResult
        {
            FileName = fileName ?? "unknown.spp",
            Functions = new List<SistemaFunction>(),
            Warnings = new List<string>()
        };

        try
        {
            var text = Encoding.UTF8.GetString(fileData);
            
            if (text.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                return ParseSistemaProjectXml(text, result);
            }

            if (text.TrimStart().StartsWith("{") || text.TrimStart().StartsWith("["))
            {
                return ParseSistemaProjectJson(text, result);
            }

            return ParseSistemaProjectBinary(fileData, result);
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"解析失败: {ex.Message}");
            return result;
        }
    }

    private SistemaLibraryResult ParseSistemaXml(string xml, SistemaLibraryResult result)
    {
        // 基础XML解析（简化版）
        var componentMatches = Regex.Matches(xml, 
            @"<Component[^>]*>(.*?)</Component>", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match match in componentMatches)
        {
            var component = new SistemaComponent();
            
            var idMatch = Regex.Match(match.Value, @"id=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (idMatch.Success) component.Id = idMatch.Groups[1].Value;

            var nameMatch = Regex.Match(match.Value, @"name=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (nameMatch.Success) component.Name = nameMatch.Groups[1].Value;

            var mttfdMatch = Regex.Match(match.Value, @"MTTFd[""']?\s*[:=]\s*([\d.]+)", RegexOptions.IgnoreCase);
            if (mttfdMatch.Success && double.TryParse(mttfdMatch.Groups[1].Value, out var mttfd))
                component.MTTFd = mttfd;

            var dcavgMatch = Regex.Match(match.Value, @"DCavg[""']?\s*[:=]\s*([\d.]+)", RegexOptions.IgnoreCase);
            if (dcavgMatch.Success && double.TryParse(dcavgMatch.Groups[1].Value, out var dcavg))
                component.DCavg = dcavg;

            result.Components.Add(component);
        }

        return result;
    }

    private SistemaLibraryResult ParseSistemaJson(string json, SistemaLibraryResult result)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("components", out var components))
            {
                foreach (var comp in components.EnumerateArray())
                {
                    var component = new SistemaComponent();
                    if (comp.TryGetProperty("id", out var id)) component.Id = id.GetString() ?? "";
                    if (comp.TryGetProperty("name", out var name)) component.Name = name.GetString() ?? "";
                    if (comp.TryGetProperty("MTTFd", out var mttfd)) component.MTTFd = mttfd.GetDouble();
                    if (comp.TryGetProperty("DCavg", out var dcavg)) component.DCavg = dcavg.GetDouble();
                    result.Components.Add(component);
                }
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"JSON解析错误: {ex.Message}");
        }

        return result;
    }

    private SistemaLibraryResult ParseSistemaBinary(byte[] data, SistemaLibraryResult result)
    {
        // 二进制格式解析（基础框架）
        // 注意：实际SISTEMA二进制格式需要详细的格式规范
        result.Warnings.Add("二进制格式解析需要详细的格式规范");
        result.Warnings.Add("建议使用SISTEMA导出为CSV或XML格式");
        return result;
    }

    private SistemaProjectResult ParseSistemaProjectXml(string xml, SistemaProjectResult result)
    {
        var functionMatches = Regex.Matches(xml,
            @"<Function[^>]*>(.*?)</Function>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match match in functionMatches)
        {
            var function = new SistemaFunction();
            
            var nameMatch = Regex.Match(match.Value, @"name=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (nameMatch.Success) function.Name = nameMatch.Groups[1].Value;

            var plMatch = Regex.Match(match.Value, @"PL[""']?\s*[:=]\s*([A-E])", RegexOptions.IgnoreCase);
            if (plMatch.Success) function.TargetPL = plMatch.Groups[1].Value;

            result.Functions.Add(function);
        }

        return result;
    }

    private SistemaProjectResult ParseSistemaProjectJson(string json, SistemaProjectResult result)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("functions", out var functions))
            {
                foreach (var func in functions.EnumerateArray())
                {
                    var function = new SistemaFunction();
                    if (func.TryGetProperty("name", out var name)) function.Name = name.GetString() ?? "";
                    if (func.TryGetProperty("targetPL", out var pl)) function.TargetPL = pl.GetString() ?? "";
                    result.Functions.Add(function);
                }
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"JSON解析错误: {ex.Message}");
        }

        return result;
    }

    private SistemaProjectResult ParseSistemaProjectBinary(byte[] data, SistemaProjectResult result)
    {
        result.Warnings.Add("二进制格式解析需要详细的格式规范");
        result.Warnings.Add("建议使用SISTEMA导出为CSV或XML格式");
        return result;
    }

    /// <summary>
    /// 将SISTEMA组件转换为系统组件格式
    /// </summary>
    public ComponentLibraryService.ComponentRecord ConvertToComponent(SistemaComponent sistemaComp)
    {
        var comp = new ComponentLibraryService.ComponentRecord
        {
            Id = sistemaComp.Id,
            Manufacturer = sistemaComp.Manufacturer ?? "Unknown",
            Model = sistemaComp.Name,
            Category = sistemaComp.Category ?? "Unknown"
        };

        comp.Parameters = new Dictionary<string, string>();
        if (sistemaComp.MTTFd.HasValue)
            comp.Parameters["MTTFd"] = sistemaComp.MTTFd.Value.ToString();
        if (sistemaComp.DCavg.HasValue)
            comp.Parameters["DCavg"] = sistemaComp.DCavg.Value.ToString();
        if (sistemaComp.PFHd.HasValue)
            comp.Parameters["PFHd"] = sistemaComp.PFHd.Value.ToString();
        if (sistemaComp.Beta.HasValue)
            comp.Parameters["beta"] = sistemaComp.Beta.Value.ToString();

        return comp;
    }
}

public class SistemaLibraryResult
{
    public string FileName { get; set; } = string.Empty;
    public List<SistemaComponent> Components { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class SistemaProjectResult
{
    public string FileName { get; set; } = string.Empty;
    public List<SistemaFunction> Functions { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class SistemaComponent
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Category { get; set; }
    public double? MTTFd { get; set; }
    public double? DCavg { get; set; }
    public double? PFHd { get; set; }
    public double? Beta { get; set; }
}

public class SistemaFunction
{
    public string Name { get; set; } = string.Empty;
    public string? TargetPL { get; set; }
}

