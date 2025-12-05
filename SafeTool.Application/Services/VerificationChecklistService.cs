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
                // 软件要求
                new Item { Code = "SW-DEV", Title = "软件开发与验证记录", Clause = "Annex A", Description = "软件生命周期过程与测试记录" },
                new Item { Code = "SW-TEST", Title = "软件测试与验证", Clause = "Annex A.2", Description = "单元测试/集成测试/系统测试记录" },
                new Item { Code = "SW-REQ", Title = "软件需求规格", Clause = "Annex A.1", Description = "软件安全需求与设计文档" },
                
                // CCF相关
                new Item { Code = "CCF-65", Title = "CCF评分≥65", Clause = "Annex F", Description = "评分清单与证据，总分需≥65" },
                new Item { Code = "CCF-EVIDENCE", Title = "CCF措施证据", Clause = "Annex F", Description = "环境分离/冗余多样化/EMC等证据" },
                
                // DCavg与故障掩蔽
                new Item { Code = "DCAVG", Title = "DCavg计算与故障掩蔽评估", Clause = "Annex K", Description = "计算路径与上限提示" },
                new Item { Code = "DCAVG-METHOD", Title = "DCavg计算方法选择", Clause = "Annex K", Description = "简化法或常规法，需说明选择理由" },
                
                // 故障排除
                new Item { Code = "FAULT-EXCL", Title = "故障排除清单", Clause = "5.2", Description = "系统化故障模式分析与排除记录" },
                new Item { Code = "FAULT-TEST", Title = "故障注入测试", Clause = "5.2.3", Description = "故障注入测试记录与结果" },
                
                // 验证计划
                new Item { Code = "VER-PLAN", Title = "验证计划", Clause = "4", Description = "验证活动计划与测试见证安排" },
                new Item { Code = "VER-RECORD", Title = "验证记录", Clause = "4.3", Description = "验证活动执行记录与结果" },
                
                // 类别与架构
                new Item { Code = "CAT-SEL", Title = "类别选择与验证", Clause = "6", Description = "Category选择理由与验证证据" },
                new Item { Code = "ARCH-VER", Title = "架构验证", Clause = "6.2", Description = "架构设计符合性验证" }
            }
            : new[]
            {
                // IEC 60204-1 完整检查表
                new Item { Code = "EARTH", Title = "接地与保护导体", Clause = "IEC60204-1 8.x", Description = "保护接地/功能接地/等电位连接的选择/连接/标识与测试" },
                new Item { Code = "ESTOP", Title = "急停设计与回路", Clause = "IEC60204-1 10.x", Description = "急停按钮位置/类型/作用范围/回路设计" },
                new Item { Code = "WIRES", Title = "导线与端子标识", Clause = "IEC60204-1 13.x", Description = "导线标识/截面选择/颜色代码/端子标识/接线图" },
                new Item { Code = "OVERLOAD", Title = "过载保护", Clause = "IEC60204-1 7.2", Description = "电动机过载保护/热继电器/熔断器选择与整定" },
                new Item { Code = "ISOLATION", Title = "隔离与断开", Clause = "IEC60204-1 5.3", Description = "主开关/隔离器/断开装置的选择与安装" },
                new Item { Code = "SHORT", Title = "短路保护", Clause = "IEC60204-1 7.2", Description = "短路保护装置选择/整定/协调" },
                new Item { Code = "EMC", Title = "EMC要求", Clause = "IEC60204-1 4.4", Description = "电磁兼容性设计/滤波/屏蔽/接地" },
                new Item { Code = "VOLTAGE", Title = "电压与频率", Clause = "IEC60204-1 4.3", Description = "电源电压/频率/波动范围符合性" },
                new Item { Code = "CABINET", Title = "电柜与外壳", Clause = "IEC60204-1 11.x", Description = "防护等级/IP等级/通风/标识" },
                new Item { Code = "LIGHTING", Title = "照明与指示", Clause = "IEC60204-1 10.3", Description = "工作照明/指示灯/信号灯设计" },
                new Item { Code = "MAINT", Title = "维护与检修", Clause = "IEC60204-1 17.x", Description = "维护通道/检修门/安全措施" },
                new Item { Code = "DOC", Title = "技术文档", Clause = "IEC60204-1 18.x", Description = "电气原理图/接线图/操作手册/维护手册" }
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

