using SafeTool.Domain.SRS;

namespace SafeTool.Application.Services;

public class SrsService
{
    private readonly Dictionary<string, SrsDocument> _store = new();

    public SrsDocument Create(SrsDocument doc)
    {
        _store[doc.Id] = doc;
        return doc;
    }

    public SrsDocument? Get(string id) => _store.TryGetValue(id, out var d) ? d : null;

    public SrsDocument? Update(string id, Action<SrsDocument> apply)
    {
        if (!_store.TryGetValue(id, out var d)) return null;
        apply(d);
        return d;
    }

    public bool Approve(string id)
    {
        var d = Get(id);
        if (d is null) return false;
        d.Status = "Approved";
        d.ApprovedAt = DateTime.UtcNow;
        return true;
    }

    public string ExportHtml(SrsDocument d)
    {
        var reqs = string.Join("", d.Requirements.Select(r => $"<li><strong>{r.Title}</strong>（{r.Category}，{(r.Mandatory ? "必需" : "可选")}）<br/>{r.Description}<br/><em>接受准则</em>：{r.AcceptanceCriteria}；<em>条款</em>：{r.ClauseRef}</li>"));
        return $@"<!doctype html><html><head><meta charset='utf-8'><title>SRS - {d.SystemName}</title>
<style>body{{font-family:Segoe UI,Arial;line-height:1.6;padding:24px}}h1,h2{{margin:0 0 8px}}code{{background:#f2f4f7;padding:2px 6px;border-radius:4px}}</style></head>
<body>
<h1>安全需求规格（SRS）</h1>
<p><strong>系统</strong>：{d.SystemName} &nbsp; <strong>版本</strong>：{d.Version} &nbsp; <strong>状态</strong>：{d.Status}</p>
<h2>关键参数</h2>
<ul>
<li>运行模式：{d.OperatingModes}</li>
<li>安全功能：{d.SafetyFunction}</li>
<li>PLr：<code>{d.RequiredPLr}</code>，架构：{d.ArchitectureCategory}，DCavg：{d.DCavg:P0}，MTTFd：{d.MTTFd:0}h</li>
<li>反应时间：{d.ReactionTime}，安全状态：{d.SafeState}</li>
<li>诊断策略：{d.DiagnosticsStrategy}</li>
<li>I/O 映射：{d.IOMap}</li>
<li>环境：{d.EnvironmentalRequirements}；EMC：{d.EMCRequirements}</li>
<li>维护与测试：{d.MaintenanceTesting}</li>
<li>CCF 措施：{d.CCFMeasures}</li>
</ul>
<h2>需求列表</h2>
<ul>{reqs}</ul>
</body></html>";
    }
}

