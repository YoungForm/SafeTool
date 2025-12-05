using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 基线管理服务（P2优先级）
/// 管理项目基线版本
/// </summary>
public class BaselineManagementService
{
    private readonly string _dataDir;

    public BaselineManagementService(string dataDir)
    {
        _dataDir = dataDir;
        EnsureDirectories();
    }

    /// <summary>
    /// 创建基线
    /// </summary>
    public Baseline CreateBaseline(string projectId, BaselineInfo info)
    {
        var baseline = new Baseline
        {
            Id = Guid.NewGuid().ToString(),
            ProjectId = projectId,
            Name = info.Name,
            Description = info.Description,
            Version = info.Version ?? "1.0.0",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = info.CreatedBy ?? "system",
            Snapshot = CreateSnapshot(projectId, info)
        };

        SaveBaseline(baseline);
        return baseline;
    }

    /// <summary>
    /// 获取基线列表
    /// </summary>
    public List<Baseline> GetBaselines(string projectId)
    {
        var path = GetBaselinesPath(projectId);
        if (!File.Exists(path))
            return new List<Baseline>();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<Baseline>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Baseline>();
        }
        catch
        {
            return new List<Baseline>();
        }
    }

    /// <summary>
    /// 获取基线
    /// </summary>
    public Baseline? GetBaseline(string projectId, string baselineId)
    {
        var baselines = GetBaselines(projectId);
        return baselines.FirstOrDefault(b => b.Id == baselineId);
    }

    /// <summary>
    /// 设置当前基线
    /// </summary>
    public bool SetCurrentBaseline(string projectId, string baselineId)
    {
        var baseline = GetBaseline(projectId, baselineId);
        if (baseline == null)
            return false;

        var currentBaseline = new CurrentBaseline
        {
            ProjectId = projectId,
            BaselineId = baselineId,
            SetAt = DateTime.UtcNow,
            SetBy = "system"
        };

        SaveCurrentBaseline(currentBaseline);
        return true;
    }

    /// <summary>
    /// 获取当前基线
    /// </summary>
    public Baseline? GetCurrentBaseline(string projectId)
    {
        var current = LoadCurrentBaseline(projectId);
        if (current == null)
            return null;

        return GetBaseline(projectId, current.BaselineId);
    }

    /// <summary>
    /// 比较基线
    /// </summary>
    public BaselineComparisonResult CompareBaselines(
        string projectId,
        string baselineId1,
        string baselineId2)
    {
        var baseline1 = GetBaseline(projectId, baselineId1);
        var baseline2 = GetBaseline(projectId, baselineId2);

        if (baseline1 == null || baseline2 == null)
        {
            throw new ArgumentException("基线不存在");
        }

        var result = new BaselineComparisonResult
        {
            Baseline1 = baseline1,
            Baseline2 = baseline2,
            ComparedAt = DateTime.UtcNow,
            Differences = new List<BaselineDifference>()
        };

        // 比较快照
        if (baseline1.Snapshot != null && baseline2.Snapshot != null)
        {
            result.Differences.AddRange(CompareSnapshots(baseline1.Snapshot, baseline2.Snapshot));
        }

        return result;
    }

    /// <summary>
    /// 恢复基线
    /// </summary>
    public bool RestoreBaseline(string projectId, string baselineId)
    {
        var baseline = GetBaseline(projectId, baselineId);
        if (baseline == null || baseline.Snapshot == null)
            return false;

        // 这里应该实现实际的恢复逻辑
        // 目前只是标记为当前基线
        return SetCurrentBaseline(projectId, baselineId);
    }

    /// <summary>
    /// 创建快照
    /// </summary>
    private BaselineSnapshot CreateSnapshot(string projectId, BaselineInfo info)
    {
        // 这里应该实际捕获项目的当前状态
        // 目前返回一个简单的快照结构
        return new BaselineSnapshot
        {
            ProjectId = projectId,
            CapturedAt = DateTime.UtcNow,
            Components = info.IncludeComponents ? new List<string>() : null,
            Functions = info.IncludeFunctions ? new List<string>() : null,
            Checklists = info.IncludeChecklists ? new List<string>() : null,
            Evidence = info.IncludeEvidence ? new List<string>() : null
        };
    }

    /// <summary>
    /// 比较快照
    /// </summary>
    private List<BaselineDifference> CompareSnapshots(
        BaselineSnapshot snapshot1,
        BaselineSnapshot snapshot2)
    {
        var differences = new List<BaselineDifference>();

        // 比较组件
        if (snapshot1.Components != null && snapshot2.Components != null)
        {
            var onlyIn1 = snapshot1.Components.Except(snapshot2.Components).ToList();
            var onlyIn2 = snapshot2.Components.Except(snapshot1.Components).ToList();

            if (onlyIn1.Any())
            {
                differences.Add(new BaselineDifference
                {
                    Type = "Component",
                    Field = "Components",
                    Value1 = string.Join(", ", onlyIn1),
                    Value2 = "不存在",
                    Description = $"基线1中有 {onlyIn1.Count} 个组件在基线2中不存在"
                });
            }

            if (onlyIn2.Any())
            {
                differences.Add(new BaselineDifference
                {
                    Type = "Component",
                    Field = "Components",
                    Value1 = "不存在",
                    Value2 = string.Join(", ", onlyIn2),
                    Description = $"基线2中有 {onlyIn2.Count} 个组件在基线1中不存在"
                });
            }
        }

        return differences;
    }

    /// <summary>
    /// 保存基线
    /// </summary>
    private void SaveBaseline(Baseline baseline)
    {
        var baselines = GetBaselines(baseline.ProjectId);
        baselines.RemoveAll(b => b.Id == baseline.Id);
        baselines.Add(baseline);
        SaveAllBaselines(baseline.ProjectId, baselines);
    }

    /// <summary>
    /// 保存所有基线
    /// </summary>
    private void SaveAllBaselines(string projectId, List<Baseline> baselines)
    {
        var path = GetBaselinesPath(projectId);
        var json = JsonSerializer.Serialize(baselines, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 保存当前基线
    /// </summary>
    private void SaveCurrentBaseline(CurrentBaseline current)
    {
        var path = GetCurrentBaselinePath(current.ProjectId);
        var json = JsonSerializer.Serialize(current, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 加载当前基线
    /// </summary>
    private CurrentBaseline? LoadCurrentBaseline(string projectId)
    {
        var path = GetCurrentBaselinePath(projectId);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<CurrentBaseline>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取基线文件路径
    /// </summary>
    private string GetBaselinesPath(string projectId)
    {
        var dir = Path.Combine(_dataDir, "baselines", projectId);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "baselines.json");
    }

    /// <summary>
    /// 获取当前基线文件路径
    /// </summary>
    private string GetCurrentBaselinePath(string projectId)
    {
        var dir = Path.Combine(_dataDir, "baselines", projectId);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "current.json");
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private void EnsureDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_dataDir, "baselines"));
    }
}

public class Baseline
{
    public string Id { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public BaselineSnapshot? Snapshot { get; set; }
}

public class BaselineInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? CreatedBy { get; set; }
    public bool IncludeComponents { get; set; } = true;
    public bool IncludeFunctions { get; set; } = true;
    public bool IncludeChecklists { get; set; } = true;
    public bool IncludeEvidence { get; set; } = true;
}

public class BaselineSnapshot
{
    public string ProjectId { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
    public List<string>? Components { get; set; }
    public List<string>? Functions { get; set; }
    public List<string>? Checklists { get; set; }
    public List<string>? Evidence { get; set; }
}

public class CurrentBaseline
{
    public string ProjectId { get; set; } = string.Empty;
    public string BaselineId { get; set; } = string.Empty;
    public DateTime SetAt { get; set; }
    public string SetBy { get; set; } = string.Empty;
}

public class BaselineComparisonResult
{
    public Baseline Baseline1 { get; set; } = new();
    public Baseline Baseline2 { get; set; } = new();
    public DateTime ComparedAt { get; set; }
    public List<BaselineDifference> Differences { get; set; } = new();
}

public class BaselineDifference
{
    public string Type { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Value1 { get; set; } = string.Empty;
    public string Value2 { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

