using System.Text;
using SafeTool.Domain.Standards;

namespace SafeTool.Application.Services;

public interface IIec62061ReportGenerator
{
    string GenerateHtml(SafeTool.Domain.Standards.SafetyFunction62061 f, SafeTool.Domain.Standards.IEC62061EvaluationResult r);
}

public class Iec62061HtmlReportGenerator : IIec62061ReportGenerator
{
    private readonly ComplianceMatrixService _matrix;
    private readonly EvidenceService _evidence;
    public Iec62061HtmlReportGenerator(ComplianceMatrixService matrix, EvidenceService evidence) { _matrix = matrix; _evidence = evidence; }

    public string GenerateHtml(SafeTool.Domain.Standards.SafetyFunction62061 f, SafeTool.Domain.Standards.IEC62061EvaluationResult r)
    {
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html><head><meta charset='utf-8'><title>IEC 62061 评估报告</title>");
        sb.Append("<style>body{font-family:Segoe UI,Arial;line-height:1.6;padding:24px}h1,h2{margin:0 0 8px}code{background:#f2f4f7;padding:2px 6px;border-radius:4px} .ok{color:#0a7} .bad{color:#b00}</style>");
        sb.Append("</head><body>");
        sb.Append($"<h1>IEC 62061 评估报告</h1><p><strong>安全功能:</strong> {f.Name} &nbsp; <strong>目标SIL:</strong> {f.TargetSIL}</p>");
        sb.Append($"<p>PFHd: <code>{r.PFHd:E2}</code>，达到SIL: <code>{r.AchievedSIL}</code></p>");
        if (f.ProofTestIntervalT1.HasValue || f.MissionTimeT10D.HasValue)
            sb.Append($"<p>T1: {f.ProofTestIntervalT1?.ToString() ?? "-"}；T10D: {f.MissionTimeT10D?.ToString() ?? "-"}</p>");
        sb.Append("<h2>子系统与组件</h2>");
        foreach (var s in f.Subsystems)
        {
            sb.Append($"<h3>{s.Name}（{s.Architecture}）</h3><ul>");
            foreach (var c in s.Components)
                sb.Append($"<li>{c.Manufacturer} {c.Model} — PFHd: <code>{c.PFHd:E2}</code>；β: {(c.Beta?.ToString() ?? "-")}</li>");
            sb.Append("</ul>");
        }
        if (r.Warnings.Count > 0)
        {
            sb.Append("<h2>提示</h2><ul>");
            foreach (var w in r.Warnings) sb.Append($"<li class='bad'>{w}</li>");
            sb.Append("</ul>");
        }
        var entries = _matrix.Get(f.Id).ToList();
        if (entries.Count > 0)
        {
            sb.Append("<h2>合规矩阵摘要</h2>");
            sb.Append("<table border='1' cellspacing='0' cellpadding='4'><thead><tr><th>标准</th><th>条款</th><th>要求</th><th>引用</th><th>证据</th><th>结果</th><th>责任人</th><th>期限</th></tr></thead><tbody>");
            foreach (var x in entries)
            {
                var ev = string.IsNullOrWhiteSpace(x.EvidenceId) ? "" : (_evidence.Get(x.EvidenceId)?.Name ?? x.EvidenceId);
                var link = string.IsNullOrWhiteSpace(x.EvidenceId) ? ev : $"<a href='/api/evidence/{x.EvidenceId}/download'>{ev}</a>";
                sb.Append($"<tr><td>{x.Standard}</td><td>{x.Clause}</td><td>{x.Requirement}</td><td>{x.Reference}</td><td>{link}</td><td>{x.Result}</td><td>{x.Owner}</td><td>{x.Due}</td></tr>");
            }
            sb.Append("</tbody></table>");
        }
        sb.Append("</body></html>");
        return sb.ToString();
    }
}
