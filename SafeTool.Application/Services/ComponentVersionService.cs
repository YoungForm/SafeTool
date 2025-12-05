using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 组件版本管理服务（版本控制模式）
/// </summary>
public class ComponentVersionService
{
    private readonly string _versionDir;
    private readonly object _lock = new();

    public ComponentVersionService(string dataDir)
    {
        _versionDir = Path.Combine(dataDir, "ComponentVersions");
        Directory.CreateDirectory(_versionDir);
    }

    /// <summary>
    /// 创建组件版本快照
    /// </summary>
    public ComponentVersion CreateVersion(string componentId, ComponentLibraryService.ComponentRecord component, string changedBy, string changeReason)
    {
        var version = new ComponentVersion
        {
            Id = Guid.NewGuid().ToString("N"),
            ComponentId = componentId,
            Version = GenerateVersionNumber(componentId),
            ComponentData = JsonSerializer.Serialize(component, new JsonSerializerOptions { WriteIndented = true }),
            ChangedBy = changedBy,
            ChangeReason = changeReason,
            CreatedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            var filePath = Path.Combine(_versionDir, $"{componentId}_{version.Version}.json");
            File.WriteAllText(filePath, JsonSerializer.Serialize(version, new JsonSerializerOptions { WriteIndented = true }));
        }

        return version;
    }

    /// <summary>
    /// 获取组件的所有版本
    /// </summary>
    public IEnumerable<ComponentVersion> GetVersions(string componentId)
    {
        lock (_lock)
        {
            var files = Directory.GetFiles(_versionDir, $"{componentId}_*.json");
            var versions = new List<ComponentVersion>();
            
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var version = JsonSerializer.Deserialize<ComponentVersion>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (version != null)
                        versions.Add(version);
                }
                catch { }
            }

            return versions.OrderByDescending(v => v.CreatedAt);
        }
    }

    /// <summary>
    /// 获取特定版本
    /// </summary>
    public ComponentVersion? GetVersion(string componentId, string version)
    {
        lock (_lock)
        {
            var filePath = Path.Combine(_versionDir, $"{componentId}_{version}.json");
            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<ComponentVersion>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// 对比两个版本
    /// </summary>
    public VersionDiff CompareVersions(string componentId, string version1, string version2)
    {
        var v1 = GetVersion(componentId, version1);
        var v2 = GetVersion(componentId, version2);

        if (v1 == null || v2 == null)
            throw new ArgumentException("版本不存在");

        var diff = new VersionDiff
        {
            ComponentId = componentId,
            Version1 = version1,
            Version2 = version2,
            Changes = new List<VersionChange>()
        };

        // 解析组件数据
        var comp1 = JsonSerializer.Deserialize<ComponentLibraryService.ComponentRecord>(v1.ComponentData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var comp2 = JsonSerializer.Deserialize<ComponentLibraryService.ComponentRecord>(v2.ComponentData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (comp1 == null || comp2 == null)
            return diff;

        // 对比字段变化
        if (comp1.Manufacturer != comp2.Manufacturer)
            diff.Changes.Add(new VersionChange { Field = "Manufacturer", OldValue = comp1.Manufacturer, NewValue = comp2.Manufacturer });

        if (comp1.Model != comp2.Model)
            diff.Changes.Add(new VersionChange { Field = "Model", OldValue = comp1.Model, NewValue = comp2.Model });

        if (comp1.Category != comp2.Category)
            diff.Changes.Add(new VersionChange { Field = "Category", OldValue = comp1.Category, NewValue = comp2.Category });

        // 对比参数变化
        var allParamKeys = comp1.Parameters.Keys.Union(comp2.Parameters.Keys).Distinct();
        foreach (var key in allParamKeys)
        {
            var oldVal = comp1.Parameters.TryGetValue(key, out var oldParamVal) ? oldParamVal : null;
            var newVal = comp2.Parameters.TryGetValue(key, out var newParamVal) ? newParamVal : null;

            if (oldVal != newVal)
            {
                diff.Changes.Add(new VersionChange
                {
                    Field = $"Parameters.{key}",
                    OldValue = oldVal ?? "(新增)",
                    NewValue = newVal ?? "(删除)"
                });
            }
        }

        return diff;
    }

    private string GenerateVersionNumber(string componentId)
    {
        var versions = GetVersions(componentId).ToList();
        if (versions.Count == 0)
            return "1.0.0";

        // 简单版本号递增：主版本.次版本.修订号
        var lastVersion = versions.First().Version;
        var parts = lastVersion.Split('.');
        if (parts.Length == 3 && int.TryParse(parts[2], out var patch))
        {
            return $"{parts[0]}.{parts[1]}.{patch + 1}";
        }

        return "1.0.0";
    }
}

public class ComponentVersion
{
    public string Id { get; set; } = string.Empty;
    public string ComponentId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string ComponentData { get; set; } = string.Empty; // JSON序列化的组件数据
    public string ChangedBy { get; set; } = string.Empty;
    public string ChangeReason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class VersionDiff
{
    public string ComponentId { get; set; } = string.Empty;
    public string Version1 { get; set; } = string.Empty;
    public string Version2 { get; set; } = string.Empty;
    public List<VersionChange> Changes { get; set; } = new();
}

public class VersionChange
{
    public string Field { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

