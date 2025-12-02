namespace SafeTool.Application.Services;

public class CcfItem
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class CcfService
{
    private readonly List<CcfItem> _items = new()
    {
        new() { Code = "CCF-ENV", Title = "环境分离与防护（温湿度/粉尘/液体）", Score = 10 },
        new() { Code = "CCF-RED", Title = "冗余多样化（不同原理/不同供应商）", Score = 20 },
        new() { Code = "CCF-WIR", Title = "布线与隔离（屏蔽/分离/抗干扰）", Score = 10 },
        new() { Code = "CCF-EMC", Title = "EMC 设计与验证（接地/滤波/试验）", Score = 15 },
        new() { Code = "CCF-MNT", Title = "维护与周期测试（诊断有效性）", Score = 10 },
        new() { Code = "CCF-DIV", Title = "逻辑与通道多样化（软件/硬件）", Score = 10 },
        new() { Code = "CCF-QA", Title = "质量流程与变更控制", Score = 10 },
        new() { Code = "CCF-DOC", Title = "文档与培训（操作/维护/故障应对）", Score = 10 },
    };

    public IEnumerable<CcfItem> GetItems() => _items;

    public int ComputeScore(IEnumerable<string> selectedCodes)
    {
        var set = new HashSet<string>(selectedCodes ?? Array.Empty<string>());
        return _items.Where(i => set.Contains(i.Code)).Sum(i => i.Score);
    }
}

