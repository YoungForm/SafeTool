using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 系统配置管理服务（P2优先级）
/// 管理系统级配置
/// </summary>
public class SystemConfigurationService
{
    private readonly string _dataDir;
    private SystemConfig _config;

    public SystemConfigurationService(string dataDir)
    {
        _dataDir = dataDir;
        EnsureDirectories();
        _config = LoadConfiguration();
    }

    /// <summary>
    /// 获取配置
    /// </summary>
    public SystemConfig GetConfiguration()
    {
        return _config;
    }

    /// <summary>
    /// 更新配置
    /// </summary>
    public SystemConfig UpdateConfiguration(SystemConfig config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        _config = config;
        SaveConfiguration(_config);
        return _config;
    }

    /// <summary>
    /// 获取配置项
    /// </summary>
    public T? GetSetting<T>(string key, T? defaultValue = default)
    {
        if (_config.Settings.ContainsKey(key))
        {
            var value = _config.Settings[key];
            if (value is JsonElement element)
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText());
            }
            return (T?)Convert.ChangeType(value, typeof(T));
        }
        return defaultValue;
    }

    /// <summary>
    /// 设置配置项
    /// </summary>
    public void SetSetting<T>(string key, T value)
    {
        _config.Settings[key] = value!;
        _config.UpdatedAt = DateTime.UtcNow;
        SaveConfiguration(_config);
    }

    /// <summary>
    /// 重置配置
    /// </summary>
    public SystemConfig ResetConfiguration()
    {
        _config = CreateDefaultConfiguration();
        SaveConfiguration(_config);
        return _config;
    }

    /// <summary>
    /// 导出配置
    /// </summary>
    public string ExportConfiguration()
    {
        return JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// 导入配置
    /// </summary>
    public SystemConfig ImportConfiguration(string json)
    {
        var config = JsonSerializer.Deserialize<SystemConfig>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (config != null)
        {
            _config = config;
            _config.UpdatedAt = DateTime.UtcNow;
            SaveConfiguration(_config);
        }
        return _config;
    }

    /// <summary>
    /// 创建默认配置
    /// </summary>
    private SystemConfig CreateDefaultConfiguration()
    {
        return new SystemConfig
        {
            Version = "1.0.0",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Settings = new Dictionary<string, object>
            {
                { "MaxFileSize", 104857600 }, // 100MB
                { "MaxUploadFiles", 10 },
                { "CacheExpiration", 3600 },
                { "SessionTimeout", 1800 },
                { "EnableAuditLog", true },
                { "EnableDataMasking", true },
                { "DefaultLanguage", "zh-CN" },
                { "DefaultTimeZone", "Asia/Shanghai" },
                { "DateFormat", "yyyy-MM-dd" },
                { "DateTimeFormat", "yyyy-MM-dd HH:mm:ss" },
                { "NumberFormat", "N2" },
                { "EnableEmailNotification", false },
                { "EnableSmsNotification", false },
                { "MaxRetryAttempts", 3 },
                { "RetryDelay", 1000 }
            }
        };
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private SystemConfig LoadConfiguration()
    {
        var path = GetConfigurationPath();
        if (!File.Exists(path))
        {
            return CreateDefaultConfiguration();
        }

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<SystemConfig>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return config ?? CreateDefaultConfiguration();
        }
        catch
        {
            return CreateDefaultConfiguration();
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    private void SaveConfiguration(SystemConfig config)
    {
        var path = GetConfigurationPath();
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 获取配置路径
    /// </summary>
    private string GetConfigurationPath()
    {
        var dir = Path.Combine(_dataDir, "system");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "config.json");
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private void EnsureDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_dataDir, "system"));
    }
}

public class SystemConfig
{
    public string Version { get; set; } = "1.0.0";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Settings { get; set; } = new();
}

