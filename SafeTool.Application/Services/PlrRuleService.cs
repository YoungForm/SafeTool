using SafeTool.Domain.Standards;

namespace SafeTool.Application.Services;

public class PlrRuleService
{
    private readonly string? _filePath;
    private readonly Dictionary<string, string> _map = new()
    {
        { "Low", "PLb" },
        { "Medium", "PLc" },
        { "High", "PLd" },
        { "Extreme", "PLe" },
    };

    public PlrRuleService() { }
    public PlrRuleService(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "plr_rules.json");
        Load();
    }

    public string EvaluateRequiredPlr(SeverityLevel s, FrequencyLevel f, AvoidanceLevel a)
    {
        var level = ISO12100Risk.RiskLevel(ISO12100Risk.RiskScore(s, f, a));
        return _map.TryGetValue(level, out var plr) ? plr : "PLc";
    }

    public Dictionary<string, string> GetRules() => new(_map);

    public void SetRules(Dictionary<string, string> rules)
    {
        foreach (var kv in rules)
            _map[kv.Key] = kv.Value;
        Save();
    }

    private void Load()
    {
        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath)) return;
        var json = File.ReadAllText(_filePath);
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (dict is null) return;
        foreach (var kv in dict) _map[kv.Key] = kv.Value;
    }

    private void Save()
    {
        if (string.IsNullOrEmpty(_filePath)) return;
        var json = System.Text.Json.JsonSerializer.Serialize(_map);
        File.WriteAllText(_filePath, json);
    }
}
