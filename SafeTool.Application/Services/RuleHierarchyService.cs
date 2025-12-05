using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 规则分层管理服务（P2优先级）
/// 支持行业/企业/项目级别的规则管理
/// </summary>
public class RuleHierarchyService
{
    private readonly string _dataDir;
    private readonly object _lock = new();

    public RuleHierarchyService(string dataDir)
    {
        _dataDir = dataDir;
        EnsureDirectories();
    }

    /// <summary>
    /// 获取规则（按优先级：项目 > 企业 > 行业）
    /// </summary>
    public RuleHierarchyResult GetRules(string? industry = null, string? enterprise = null, string? project = null)
    {
        var result = new RuleHierarchyResult
        {
            Industry = industry,
            Enterprise = enterprise,
            Project = project,
            Rules = new List<RuleItem>()
        };

        // 1. 加载行业规则
        if (!string.IsNullOrEmpty(industry))
        {
            var industryRules = LoadRules("industry", industry);
            result.Rules.AddRange(industryRules);
            result.IndustryRulesCount = industryRules.Count;
        }

        // 2. 加载企业规则（覆盖行业规则）
        if (!string.IsNullOrEmpty(enterprise))
        {
            var enterpriseRules = LoadRules("enterprise", enterprise);
            result.Rules.AddRange(enterpriseRules);
            result.EnterpriseRulesCount = enterpriseRules.Count;
        }

        // 3. 加载项目规则（覆盖企业和行业规则）
        if (!string.IsNullOrEmpty(project))
        {
            var projectRules = LoadRules("project", project);
            result.Rules.AddRange(projectRules);
            result.ProjectRulesCount = projectRules.Count;
        }

        // 4. 去重（项目规则优先）
        result.Rules = result.Rules
            .GroupBy(r => r.RuleKey)
            .Select(g => g.OrderByDescending(r => GetRulePriority(r.Level)).First())
            .ToList();

        result.TotalRulesCount = result.Rules.Count;

        return result;
    }

    /// <summary>
    /// 创建或更新规则
    /// </summary>
    public RuleItem CreateOrUpdateRule(string level, string levelId, RuleItem rule)
    {
        rule.Level = level;
        rule.LevelId = levelId;
        rule.UpdatedAt = DateTime.UtcNow;

        var rules = LoadRules(level, levelId);
        var existing = rules.FirstOrDefault(r => r.RuleKey == rule.RuleKey);

        if (existing != null)
        {
            rules.Remove(existing);
        }

        rules.Add(rule);
        SaveRules(level, levelId, rules);

        return rule;
    }

    /// <summary>
    /// 删除规则
    /// </summary>
    public bool DeleteRule(string level, string levelId, string ruleKey)
    {
        var rules = LoadRules(level, levelId);
        var rule = rules.FirstOrDefault(r => r.RuleKey == ruleKey);

        if (rule != null)
        {
            rules.Remove(rule);
            SaveRules(level, levelId, rules);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 比较规则差异
    /// </summary>
    public RuleDifferenceResult CompareRules(
        string level1, string levelId1,
        string level2, string levelId2)
    {
        var rules1 = LoadRules(level1, levelId1);
        var rules2 = LoadRules(level2, levelId2);

        var result = new RuleDifferenceResult
        {
            Level1 = $"{level1}:{levelId1}",
            Level2 = $"{level2}:{levelId2}",
            ComparedAt = DateTime.UtcNow
        };

        var keys1 = rules1.Select(r => r.RuleKey).ToHashSet();
        var keys2 = rules2.Select(r => r.RuleKey).ToHashSet();

        // 仅在level1中的规则
        result.OnlyInLevel1 = rules1
            .Where(r => !keys2.Contains(r.RuleKey))
            .Select(r => r.RuleKey)
            .ToList();

        // 仅在level2中的规则
        result.OnlyInLevel2 = rules2
            .Where(r => !keys1.Contains(r.RuleKey))
            .Select(r => r.RuleKey)
            .ToList();

        // 两个级别都有的规则（可能值不同）
        var commonKeys = keys1.Intersect(keys2);
        foreach (var key in commonKeys)
        {
            var rule1 = rules1.First(r => r.RuleKey == key);
            var rule2 = rules2.First(r => r.RuleKey == key);

            if (JsonSerializer.Serialize(rule1) != JsonSerializer.Serialize(rule2))
            {
                result.DifferentValues.Add(new RuleDifference
                {
                    RuleKey = key,
                    Value1 = JsonSerializer.Serialize(rule1),
                    Value2 = JsonSerializer.Serialize(rule2)
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 加载规则
    /// </summary>
    private List<RuleItem> LoadRules(string level, string levelId)
    {
        var path = GetRulesPath(level, levelId);
        if (!File.Exists(path))
            return new List<RuleItem>();

        try
        {
            var json = File.ReadAllText(path);
            var rules = JsonSerializer.Deserialize<List<RuleItem>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return rules ?? new List<RuleItem>();
        }
        catch
        {
            return new List<RuleItem>();
        }
    }

    /// <summary>
    /// 保存规则
    /// </summary>
    private void SaveRules(string level, string levelId, List<RuleItem> rules)
    {
        lock (_lock)
        {
            var path = GetRulesPath(level, levelId);
            var json = JsonSerializer.Serialize(rules, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }

    /// <summary>
    /// 获取规则路径
    /// </summary>
    private string GetRulesPath(string level, string levelId)
    {
        var dir = Path.Combine(_dataDir, "rules", level);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{levelId}.json");
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private void EnsureDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_dataDir, "rules", "industry"));
        Directory.CreateDirectory(Path.Combine(_dataDir, "rules", "enterprise"));
        Directory.CreateDirectory(Path.Combine(_dataDir, "rules", "project"));
    }

    /// <summary>
    /// 获取规则优先级
    /// </summary>
    private int GetRulePriority(string level)
    {
        return level switch
        {
            "project" => 3,
            "enterprise" => 2,
            "industry" => 1,
            _ => 0
        };
    }
}

public class RuleHierarchyResult
{
    public string? Industry { get; set; }
    public string? Enterprise { get; set; }
    public string? Project { get; set; }
    public int IndustryRulesCount { get; set; }
    public int EnterpriseRulesCount { get; set; }
    public int ProjectRulesCount { get; set; }
    public int TotalRulesCount { get; set; }
    public List<RuleItem> Rules { get; set; } = new();
}

public class RuleItem
{
    public string RuleKey { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty; // industry/enterprise/project
    public string LevelId { get; set; } = string.Empty;
    public Dictionary<string, object> RuleData { get; set; } = new();
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class RuleDifferenceResult
{
    public string Level1 { get; set; } = string.Empty;
    public string Level2 { get; set; } = string.Empty;
    public DateTime ComparedAt { get; set; }
    public List<string> OnlyInLevel1 { get; set; } = new();
    public List<string> OnlyInLevel2 { get; set; } = new();
    public List<RuleDifference> DifferentValues { get; set; } = new();
}

public class RuleDifference
{
    public string RuleKey { get; set; } = string.Empty;
    public string Value1 { get; set; } = string.Empty;
    public string Value2 { get; set; } = string.Empty;
}

