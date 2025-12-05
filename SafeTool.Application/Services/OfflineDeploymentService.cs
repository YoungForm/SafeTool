using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 离线/内网部署配置服务（P2优先级）
/// 管理离线部署配置和文档
/// </summary>
public class OfflineDeploymentService
{
    private readonly string _dataDir;

    public OfflineDeploymentService(string dataDir)
    {
        _dataDir = dataDir;
        EnsureDirectories();
    }

    /// <summary>
    /// 生成部署配置
    /// </summary>
    public DeploymentConfiguration GenerateConfiguration(DeploymentType type)
    {
        var config = new DeploymentConfiguration
        {
            Type = type,
            GeneratedAt = DateTime.UtcNow,
            Settings = new Dictionary<string, object>()
        };

        switch (type)
        {
            case DeploymentType.Offline:
                config.Settings = GenerateOfflineSettings();
                break;
            case DeploymentType.Intranet:
                config.Settings = GenerateIntranetSettings();
                break;
            case DeploymentType.Hybrid:
                config.Settings = GenerateHybridSettings();
                break;
        }

        config.Documentation = GenerateDocumentation(type);
        SaveConfiguration(config);

        return config;
    }

    /// <summary>
    /// 获取部署配置
    /// </summary>
    public DeploymentConfiguration? GetConfiguration(DeploymentType type)
    {
        var path = GetConfigurationPath(type);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<DeploymentConfiguration>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 验证部署配置
    /// </summary>
    public DeploymentValidationResult ValidateConfiguration(DeploymentConfiguration config)
    {
        var result = new DeploymentValidationResult
        {
            IsValid = true,
            Issues = new List<string>(),
            Recommendations = new List<string>()
        };

        // 验证必需设置
        var requiredSettings = GetRequiredSettings(config.Type);
        foreach (var setting in requiredSettings)
        {
            if (!config.Settings.ContainsKey(setting))
            {
                result.IsValid = false;
                result.Issues.Add($"缺少必需设置: {setting}");
            }
        }

        // 验证数据存储路径
        if (config.Settings.ContainsKey("DataPath"))
        {
            var dataPath = config.Settings["DataPath"]?.ToString();
            if (string.IsNullOrEmpty(dataPath))
            {
                result.IsValid = false;
                result.Issues.Add("数据存储路径不能为空");
            }
            else if (!Directory.Exists(dataPath))
            {
                result.Recommendations.Add($"数据存储路径不存在，将自动创建: {dataPath}");
            }
        }

        // 验证网络设置（内网部署）
        if (config.Type == DeploymentType.Intranet || config.Type == DeploymentType.Hybrid)
        {
            if (!config.Settings.ContainsKey("NetworkAddress"))
            {
                result.IsValid = false;
                result.Issues.Add("内网部署需要配置网络地址");
            }
        }

        if (result.IsValid && !result.Issues.Any())
        {
            result.Message = "部署配置有效";
        }

        return result;
    }

    /// <summary>
    /// 生成部署文档
    /// </summary>
    public string GenerateDeploymentDocument(DeploymentType type)
    {
        var config = GetConfiguration(type) ?? GenerateConfiguration(type);
        var doc = new System.Text.StringBuilder();

        doc.AppendLine($"# {type} 部署配置文档");
        doc.AppendLine();
        doc.AppendLine($"生成时间: {config.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        doc.AppendLine();

        doc.AppendLine("## 一、部署类型");
        doc.AppendLine();
        doc.AppendLine($"- **类型**: {type}");
        doc.AppendLine($"- **说明**: {GetDeploymentTypeDescription(type)}");
        doc.AppendLine();

        doc.AppendLine("## 二、配置设置");
        doc.AppendLine();
        foreach (var setting in config.Settings)
        {
            doc.AppendLine($"- **{setting.Key}**: {setting.Value}");
        }
        doc.AppendLine();

        doc.AppendLine("## 三、部署步骤");
        doc.AppendLine();
        var steps = GetDeploymentSteps(type);
        for (int i = 0; i < steps.Count; i++)
        {
            doc.AppendLine($"{i + 1}. {steps[i]}");
        }
        doc.AppendLine();

        doc.AppendLine("## 四、注意事项");
        doc.AppendLine();
        var notes = GetDeploymentNotes(type);
        foreach (var note in notes)
        {
            doc.AppendLine($"- {note}");
        }
        doc.AppendLine();

        doc.AppendLine("## 五、故障排查");
        doc.AppendLine();
        var troubleshooting = GetTroubleshooting(type);
        foreach (var item in troubleshooting)
        {
            doc.AppendLine($"### {item.Key}");
            doc.AppendLine();
            doc.AppendLine(item.Value);
            doc.AppendLine();
        }

        return doc.ToString();
    }

    /// <summary>
    /// 生成离线设置
    /// </summary>
    private Dictionary<string, object> GenerateOfflineSettings()
    {
        return new Dictionary<string, object>
        {
            { "DataPath", Path.Combine(_dataDir, "offline-data") },
            { "EnableExternalApi", false },
            { "EnableCloudSync", false },
            { "CacheExpiration", 3600 },
            { "MaxCacheSize", "1GB" },
            { "EnableLocalDatabase", true },
            { "DatabasePath", Path.Combine(_dataDir, "offline-db") }
        };
    }

    /// <summary>
    /// 生成内网设置
    /// </summary>
    private Dictionary<string, object> GenerateIntranetSettings()
    {
        return new Dictionary<string, object>
        {
            { "DataPath", Path.Combine(_dataDir, "intranet-data") },
            { "NetworkAddress", "http://localhost:5000" },
            { "EnableExternalApi", false },
            { "EnableCloudSync", false },
            { "InternalNetworkOnly", true },
            { "EnableLocalDatabase", true },
            { "DatabasePath", Path.Combine(_dataDir, "intranet-db") }
        };
    }

    /// <summary>
    /// 生成混合设置
    /// </summary>
    private Dictionary<string, object> GenerateHybridSettings()
    {
        return new Dictionary<string, object>
        {
            { "DataPath", Path.Combine(_dataDir, "hybrid-data") },
            { "NetworkAddress", "http://localhost:5000" },
            { "EnableExternalApi", true },
            { "EnableCloudSync", false },
            { "InternalNetworkOnly", false },
            { "EnableLocalDatabase", true },
            { "DatabasePath", Path.Combine(_dataDir, "hybrid-db") }
        };
    }

    /// <summary>
    /// 生成文档
    /// </summary>
    private DeploymentDocumentation GenerateDocumentation(DeploymentType type)
    {
        return new DeploymentDocumentation
        {
            Type = type,
            Description = GetDeploymentTypeDescription(type),
            Steps = GetDeploymentSteps(type),
            Notes = GetDeploymentNotes(type),
            Troubleshooting = GetTroubleshooting(type)
        };
    }

    /// <summary>
    /// 获取部署类型说明
    /// </summary>
    private string GetDeploymentTypeDescription(DeploymentType type)
    {
        return type switch
        {
            DeploymentType.Offline => "完全离线部署，不依赖外部网络连接",
            DeploymentType.Intranet => "内网部署，仅在内网环境中运行",
            DeploymentType.Hybrid => "混合部署，支持内网和有限的外部访问",
            _ => "未知部署类型"
        };
    }

    /// <summary>
    /// 获取部署步骤
    /// </summary>
    private List<string> GetDeploymentSteps(DeploymentType type)
    {
        var steps = new List<string>
        {
            "准备部署环境（服务器、操作系统）",
            "安装.NET运行时环境",
            "配置数据存储路径",
            "设置应用程序配置"
        };

        if (type == DeploymentType.Intranet || type == DeploymentType.Hybrid)
        {
            steps.Add("配置网络地址和端口");
            steps.Add("配置防火墙规则");
        }

        steps.AddRange(new[]
        {
            "部署应用程序文件",
            "初始化数据存储",
            "启动应用程序",
            "验证部署状态"
        });

        return steps;
    }

    /// <summary>
    /// 获取部署注意事项
    /// </summary>
    private List<string> GetDeploymentNotes(DeploymentType type)
    {
        var notes = new List<string>
        {
            "确保数据存储路径有足够的磁盘空间",
            "定期备份数据存储目录",
            "监控应用程序日志"
        };

        if (type == DeploymentType.Offline)
        {
            notes.Add("离线部署无法使用外部API服务");
            notes.Add("需要手动更新组件库和规则库");
        }

        if (type == DeploymentType.Intranet)
        {
            notes.Add("确保内网环境安全");
            notes.Add("配置适当的访问控制");
        }

        return notes;
    }

    /// <summary>
    /// 获取故障排查信息
    /// </summary>
    private Dictionary<string, string> GetTroubleshooting(DeploymentType type)
    {
        return new Dictionary<string, string>
        {
            { "应用程序无法启动", "检查.NET运行时是否正确安装，查看应用程序日志" },
            { "数据存储错误", "检查数据存储路径权限，确保有读写权限" },
            { "网络连接问题", type == DeploymentType.Offline ? "离线部署不支持网络连接" : "检查网络配置和防火墙设置" }
        };
    }

    /// <summary>
    /// 获取必需设置
    /// </summary>
    private List<string> GetRequiredSettings(DeploymentType type)
    {
        var settings = new List<string> { "DataPath" };

        if (type == DeploymentType.Intranet || type == DeploymentType.Hybrid)
        {
            settings.Add("NetworkAddress");
        }

        return settings;
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    private void SaveConfiguration(DeploymentConfiguration config)
    {
        var path = GetConfigurationPath(config.Type);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 获取配置路径
    /// </summary>
    private string GetConfigurationPath(DeploymentType type)
    {
        var dir = Path.Combine(_dataDir, "deployment");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{type}.json");
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private void EnsureDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_dataDir, "deployment"));
    }
}

public enum DeploymentType
{
    Offline,
    Intranet,
    Hybrid
}

public class DeploymentConfiguration
{
    public DeploymentType Type { get; set; }
    public DateTime GeneratedAt { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
    public DeploymentDocumentation? Documentation { get; set; }
}

public class DeploymentDocumentation
{
    public DeploymentType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = new();
    public List<string> Notes { get; set; } = new();
    public Dictionary<string, string> Troubleshooting { get; set; } = new();
}

public class DeploymentValidationResult
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

