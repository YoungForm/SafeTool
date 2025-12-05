using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 组件环境与应用限制服务（P2优先级）
/// 管理组件的环境条件与应用限制
/// </summary>
public class ComponentEnvironmentService
{
    private readonly ComponentLibraryService _componentLibrary;
    private readonly string _dataDir;

    public ComponentEnvironmentService(ComponentLibraryService componentLibrary, string dataDir)
    {
        _componentLibrary = componentLibrary;
        _dataDir = dataDir;
        EnsureDirectories();
    }

    /// <summary>
    /// 设置组件环境限制
    /// </summary>
    public ComponentEnvironmentLimits SetEnvironmentLimits(
        string componentId,
        ComponentEnvironmentLimits limits)
    {
        var component = _componentLibrary.Get(componentId);
        if (component == null)
            throw new ArgumentException($"组件不存在: {componentId}");

        limits.ComponentId = componentId;
        limits.UpdatedAt = DateTime.UtcNow;

        SaveEnvironmentLimits(limits);
        return limits;
    }

    /// <summary>
    /// 获取组件环境限制
    /// </summary>
    public ComponentEnvironmentLimits? GetEnvironmentLimits(string componentId)
    {
        var path = GetLimitsPath(componentId);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ComponentEnvironmentLimits>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 验证环境条件
    /// </summary>
    public EnvironmentValidationResult ValidateEnvironment(
        string componentId,
        EnvironmentConditions conditions)
    {
        var result = new EnvironmentValidationResult
        {
            ComponentId = componentId,
            ValidatedAt = DateTime.UtcNow,
            IsValid = true,
            Issues = new List<string>()
        };

        var limits = GetEnvironmentLimits(componentId);
        if (limits == null)
        {
            result.IsValid = false;
            result.Issues.Add("组件未设置环境限制");
            return result;
        }

        // 温度验证
        if (limits.TemperatureMin.HasValue && conditions.Temperature < limits.TemperatureMin.Value)
        {
            result.IsValid = false;
            result.Issues.Add($"温度过低: {conditions.Temperature}°C < {limits.TemperatureMin.Value}°C");
        }
        if (limits.TemperatureMax.HasValue && conditions.Temperature > limits.TemperatureMax.Value)
        {
            result.IsValid = false;
            result.Issues.Add($"温度过高: {conditions.Temperature}°C > {limits.TemperatureMax.Value}°C");
        }

        // 湿度验证
        if (limits.HumidityMin.HasValue && conditions.Humidity < limits.HumidityMin.Value)
        {
            result.IsValid = false;
            result.Issues.Add($"湿度过低: {conditions.Humidity}% < {limits.HumidityMin.Value}%");
        }
        if (limits.HumidityMax.HasValue && conditions.Humidity > limits.HumidityMax.Value)
        {
            result.IsValid = false;
            result.Issues.Add($"湿度过高: {conditions.Humidity}% > {limits.HumidityMax.Value}%");
        }

        // 振动验证
        if (limits.VibrationMax.HasValue && conditions.Vibration > limits.VibrationMax.Value)
        {
            result.IsValid = false;
            result.Issues.Add($"振动过大: {conditions.Vibration} m/s² > {limits.VibrationMax.Value} m/s²");
        }

        // 应用限制验证
        if (limits.ApplicationRestrictions != null && limits.ApplicationRestrictions.Any())
        {
            foreach (var restriction in limits.ApplicationRestrictions)
            {
                if (!string.IsNullOrEmpty(restriction.Condition) &&
                    conditions.ApplicationContext != null &&
                    conditions.ApplicationContext.Contains(restriction.Condition))
                {
                    result.IsValid = false;
                    result.Issues.Add($"应用限制: {restriction.Description}");
                }
            }
        }

        if (result.IsValid)
        {
            result.Message = "环境条件满足要求";
        }

        return result;
    }

    /// <summary>
    /// 获取适用组件列表
    /// </summary>
    public List<ComponentCompatibility> GetCompatibleComponents(EnvironmentConditions conditions)
    {
        var compatible = new List<ComponentCompatibility>();
        var allComponents = _componentLibrary.List();

        foreach (var component in allComponents)
        {
            var limits = GetEnvironmentLimits(component.Id);
            if (limits == null)
                continue;

            var validation = ValidateEnvironment(component.Id, conditions);
            compatible.Add(new ComponentCompatibility
            {
                ComponentId = component.Id,
                ComponentName = component.Name,
                Manufacturer = component.Manufacturer,
                Model = component.Model,
                IsCompatible = validation.IsValid,
                Issues = validation.Issues,
                Limits = limits
            });
        }

        return compatible.OrderBy(c => c.IsCompatible ? 0 : 1).ToList();
    }

    /// <summary>
    /// 保存环境限制
    /// </summary>
    private void SaveEnvironmentLimits(ComponentEnvironmentLimits limits)
    {
        var path = GetLimitsPath(limits.ComponentId);
        var json = JsonSerializer.Serialize(limits, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 获取限制文件路径
    /// </summary>
    private string GetLimitsPath(string componentId)
    {
        var dir = Path.Combine(_dataDir, "component-environments");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{componentId}.json");
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private void EnsureDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_dataDir, "component-environments"));
    }
}

public class ComponentEnvironmentLimits
{
    public string ComponentId { get; set; } = string.Empty;
    public double? TemperatureMin { get; set; } // °C
    public double? TemperatureMax { get; set; } // °C
    public double? HumidityMin { get; set; } // %
    public double? HumidityMax { get; set; } // %
    public double? VibrationMax { get; set; } // m/s²
    public double? PressureMin { get; set; } // kPa
    public double? PressureMax { get; set; } // kPa
    public List<ApplicationRestriction> ApplicationRestrictions { get; set; } = new();
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ApplicationRestriction
{
    public string Condition { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning"; // Warning/Error
}

public class EnvironmentConditions
{
    public double Temperature { get; set; } // °C
    public double Humidity { get; set; } // %
    public double? Vibration { get; set; } // m/s²
    public double? Pressure { get; set; } // kPa
    public string? ApplicationContext { get; set; }
}

public class EnvironmentValidationResult
{
    public string ComponentId { get; set; } = string.Empty;
    public DateTime ValidatedAt { get; set; }
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class ComponentCompatibility
{
    public string ComponentId { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool IsCompatible { get; set; }
    public List<string> Issues { get; set; } = new();
    public ComponentEnvironmentLimits? Limits { get; set; }
}

