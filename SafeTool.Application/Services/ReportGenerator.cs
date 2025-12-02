using System.Text;
using SafeTool.Domain.Compliance;
using SafeTool.Domain.Standards;

namespace SafeTool.Application.Services;

public interface IReportGenerator
{
    string GenerateHtml(ComplianceChecklist checklist, EvaluationResult result);
}

public class HtmlReportGenerator : IReportGenerator
{
    public string GenerateHtml(ComplianceChecklist c, EvaluationResult r)
    {
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html><head><meta charset='utf-8'><title>合规自检报告</title>");
        sb.Append("<style>body{font-family:Segoe UI,Arial;line-height:1.6;padding:24px}h1,h2{margin:0 0 8px}code{background:#f2f4f7;padding:2px 6px;border-radius:4px} .ok{color:#0a7} .bad{color:#b00}</style>");
        sb.Append("</head><body>");
        sb.Append($"<h1>合规自检报告</h1><p><strong>系统:</strong> {c.SystemName} &nbsp; <strong>评估人:</strong> {c.Assessor} &nbsp; <strong>日期:</strong> {c.AssessmentDate:yyyy-MM-dd}</p>");
        sb.Append($"<p><strong>结论:</strong> <span class='" + (r.IsCompliant ? "ok" : "bad") + "'>" + r.Summary + "</span></p>");

        sb.Append("<h2>ISO 12100 风险评估</h2>");
        var score = ISO12100Risk.RiskScore(c.ISO12100.Severity, c.ISO12100.Frequency, c.ISO12100.Avoidance);
        var level = ISO12100Risk.RiskLevel(score);
        sb.Append($"<p>危害: {string.Join(", ", c.ISO12100.IdentifiedHazards)}</p>");
        sb.Append($"<p>风险评分: <code>{score}</code>，风险等级: <code>{level}</code></p>");
        if (!string.IsNullOrWhiteSpace(c.ISO12100.RiskReductionMeasures))
            sb.Append($"<p>风险降低措施: {c.ISO12100.RiskReductionMeasures}</p>");

        sb.Append("<h2>ISO 13849-1 性能等级</h2>");
        var achieved = ISO13849Calculator.AchievedPL(c.ISO13849);
        sb.Append($"<p>架构: {c.ISO13849.Architecture}，DCavg: {c.ISO13849.DCavg:P0}，MTTFd: {c.ISO13849.MTTFd:0}h，CCF: {c.ISO13849.CCFScore}</p>");
        sb.Append($"<p>所需PL: <code>{c.ISO13849.RequiredPL}</code>，达到PL: <code>{achieved}</code>，验证完成: {(c.ISO13849.ValidationPerformed ? "是" : "否")}</p>");

        sb.Append("<h2>一般合规项</h2><ul>");
        foreach (var item in c.GeneralItems)
        {
            sb.Append($"<li>{(item.Required ? "[必需]" : "[可选]")} {item.Title} - {(item.Completed ? "完成" : "未完成")}{(string.IsNullOrWhiteSpace(item.Evidence) ? string.Empty : $"；证据: {item.Evidence}")}</li>");
        }
        sb.Append("</ul>");

        if (r.NonConformities.Count > 0)
        {
            sb.Append("<h2>不符合项</h2><ul>");
            foreach (var n in r.NonConformities) sb.Append($"<li class='bad'>{n}</li>");
            sb.Append("</ul>");
            sb.Append($"<p><strong>整改建议:</strong> {r.RecommendedActions}</p>");
        }

        sb.Append("<h2>SRS 摘要</h2><p>如项目已创建 SRS，请在系统中联动导出完整 SRS 文档，并确保需求与评估、验证形成双向追溯。核心条款参考：ISO 13849-1（类别、DCavg/MTTFd、CCF≥65、验证），ISO 12100（风险评估与三步法）。</p>");

        sb.Append("</body></html>");
        return sb.ToString();
    }
}

