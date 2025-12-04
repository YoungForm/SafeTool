using System.Text.Json;

namespace SafeTool.Application.Services;

public class VerificationChecklistService
{
    private readonly string _path;
    private readonly object _lock = new();
    private Data _data = new();

    public VerificationChecklistService(string dataDir)
    {
        _path = Path.Combine(dataDir, "verification.json");
        if (File.Exists(_path))
        {
            var json = File.ReadAllText(_path);
            var d = JsonSerializer.Deserialize<Data>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (d is not null) _data = d;
        }
        else Persist();
    }

    public IEnumerable<Item> Get(string projectId, string standard)
    {
        lock (_lock)
        {
            return _data.Items.TryGetValue(projectId, out var byStd) && byStd.TryGetValue(standard, out var list) ? list : Enumerable.Empty<Item>();
        }
    }

    public Item Upsert(string projectId, string standard, Item item)
    {
        lock (_lock)
        {
            if (!_data.Items.TryGetValue(projectId, out var byStd)) { byStd = new(); _data.Items[projectId] = byStd; }
            if (!byStd.TryGetValue(standard, out var list)) { list = new List<Item>(); byStd[standard] = list; }
            item.Id = item.Id ?? Guid.NewGuid().ToString("N"); item.Standard = standard;
            list.RemoveAll(x => x.Id == item.Id);
            list.Add(item);
            Persist();
            return item;
        }
    }

    public IEnumerable<Item> Seed(string projectId, string standard)
    {
        var presets = standard.Equals("ISO13849-2", StringComparison.OrdinalIgnoreCase)
            ? new[]
            {
                new Item { Code = "SW-DEV", Title = "软件开发与验证记录", Clause = "Annex A", Description = "过程与测试记录" },
                new Item { Code = "CCF-65", Title = "CCF评分≥65", Clause = "Annex F", Description = "评分清单与证据" },
                new Item { Code = "DCAVG", Title = "DCavg计算与故障掩蔽评估", Clause = "Annex K", Description = "计算路径与上限提示" }
            }
            : new[]
            {
                new Item { Code = "EARTH", Title = "接地与保护导体", Clause = "IEC60204-1 8.x", Description = "选择/连接/标识与测试" },
                new Item { Code = "ESTOP", Title = "急停设计与回路", Clause = "IEC60204-1 10.x", Description = "位置/类型/作用范围" },
                new Item { Code = "WIRES", Title = "导线与端子标识", Clause = "IEC60204-1 13.x", Description = "标识/截面/颜色/接线图" }
            };
        foreach (var it in presets) Upsert(projectId, standard, it);
        return Get(projectId, standard);
    }

    private void Persist()
    {
        var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }

    public class Data
    {
        public Dictionary<string, Dictionary<string, List<Item>>> Items { get; set; } = new();
    }

    public class Item
    {
        public string? Id { get; set; }
        public string Standard { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Clause { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? EvidenceId { get; set; }
        public string Result { get; set; } = "pending"; // pending/pass/fail
        public string? Owner { get; set; }
        public string? Due { get; set; }
    }
}

