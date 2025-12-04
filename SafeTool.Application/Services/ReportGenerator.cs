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
    private readonly ComplianceMatrixService _matrix;
    private readonly EvidenceService _evidence;
    private readonly VerificationChecklistService _verify;
    public HtmlReportGenerator(ComplianceMatrixService matrix, EvidenceService evidence, VerificationChecklistService verify) { _matrix = matrix; _evidence = evidence; _verify = verify; }

    public string GenerateHtml(ComplianceChecklist c, EvaluationResult r)
    {
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html><head><meta charset='utf-8'><title>合规自检报告 Compliance Report</title>");
        sb.Append("<style>body{font-family:Segoe UI,Arial;line-height:1.6;padding:24px}h1,h2{margin:0 0 8px}code{background:#f2f4f7;padding:2px 6px;border-radius:4px} .ok{color:#0a7} .bad{color:#b00}</style>");
        sb.Append("</head><body>");
        sb.Append($"<h1 id='top'>合规自检报告（Compliance Report）</h1><p><strong>系统（System）:</strong> {c.SystemName} &nbsp; <strong>评估人（Assessor）:</strong> {c.Assessor} &nbsp; <strong>日期（Date）:</strong> {c.AssessmentDate:yyyy-MM-dd}</p>");
        sb.Append("<div style='margin:12px 0;padding:8px;border:1px solid #e5e7eb'><strong>目录（Contents）:</strong> ");
        sb.Append("<a href='#iso12100' style='margin-right:8px'>ISO 12100</a>");
        sb.Append("<a href='#iso13849' style='margin-right:8px'>ISO 13849-1</a>");
        sb.Append("<a href='#general' style='margin-right:8px'>一般合规项</a>");
        sb.Append("<a href='#nonconform' style='margin-right:8px'>不符合项</a>");
        sb.Append("<a href='#matrix' style='margin-right:8px'>合规矩阵摘要</a>");
        sb.Append("<a href='#clauses' style='margin-right:8px'>条款索引</a>");
        sb.Append("<a href='#srs' style='margin-right:8px'>SRS 摘要</a>");
        sb.Append("</div>");
        sb.Append($"<p><strong>结论:</strong> <span class='" + (r.IsCompliant ? "ok" : "bad") + "'>" + r.Summary + "</span></p>");

        sb.Append("<h2 id='iso12100'>ISO 12100 风险评估（Risk Assessment）</h2>");
        var score = ISO12100Risk.RiskScore(c.ISO12100.Severity, c.ISO12100.Frequency, c.ISO12100.Avoidance);
        var level = ISO12100Risk.RiskLevel(score);
        sb.Append($"<p>危害: {string.Join(", ", c.ISO12100.IdentifiedHazards)}</p>");
        sb.Append($"<p>风险评分: <code>{score}</code>，风险等级: <code>{level}</code></p>");
        if (!string.IsNullOrWhiteSpace(c.ISO12100.RiskReductionMeasures))
            sb.Append($"<p>风险降低措施: {c.ISO12100.RiskReductionMeasures}</p>");

        sb.Append("<h2 id='iso13849'>ISO 13849-1 性能等级（Performance Level）</h2>");
        var achieved = ISO13849Calculator.AchievedPL(c.ISO13849);
        sb.Append($"<p>架构: {c.ISO13849.Architecture}，DCavg: {c.ISO13849.DCavg:P0}，MTTFd: {c.ISO13849.MTTFd:0}h，CCF: {c.ISO13849.CCFScore}</p>");
        sb.Append($"<p>所需PL: <code>{c.ISO13849.RequiredPL}</code>，达到PL: <code>{achieved}</code>，验证完成: {(c.ISO13849.ValidationPerformed ? "是" : "否")}</p>");

        sb.Append("<h2 id='general'>一般合规项（General）</h2><ul>");
        foreach (var item in c.GeneralItems)
        {
            sb.Append($"<li>{(item.Required ? "[必需]" : "[可选]")} {item.Title} - {(item.Completed ? "完成" : "未完成")}{(string.IsNullOrWhiteSpace(item.Evidence) ? string.Empty : $"；证据: {item.Evidence}")}</li>");
        }
        sb.Append("</ul>");

        if (r.NonConformities.Count > 0)
        {
            sb.Append("<h2 id='nonconform'>不符合项（Non-conformities）</h2><ul>");
            foreach (var n in r.NonConformities) sb.Append($"<li class='bad'>{n}</li>");
            sb.Append("</ul>");
            sb.Append($"<p><strong>整改建议:</strong> {r.RecommendedActions}</p>");
        }

        var pid = string.IsNullOrWhiteSpace(c.ProjectId) ? c.SystemName : c.ProjectId;
        var entries = _matrix.Get(pid).ToList();
        if (entries.Count > 0)
        {
            sb.Append("<h2 id='matrix'>合规矩阵摘要（Compliance Matrix Summary）</h2>");
            sb.Append("<table border='1' cellspacing='0' cellpadding='4'><thead><tr><th>标准（Standard）</th><th>条款（Clause）</th><th>要求（Requirement）</th><th>引用（Reference）</th><th>证据（Evidence）</th><th>结果（Result）</th><th>责任人（Owner）</th><th>期限（Due）</th></tr></thead><tbody>");
            foreach (var x in entries)
            {
                var ev = string.IsNullOrWhiteSpace(x.EvidenceId) ? "" : (_evidence.Get(x.EvidenceId)?.Name ?? x.EvidenceId);
                var link = string.IsNullOrWhiteSpace(x.EvidenceId) ? ev : $"<a href='/api/evidence/{x.EvidenceId}/download'>{ev}</a>";
                sb.Append($"<tr><td>{x.Standard}</td><td>{x.Clause}</td><td>{x.Requirement}</td><td>{x.Reference}</td><td>{link}</td><td>{x.Result}</td><td>{x.Owner}</td><td>{x.Due}</td></tr>");
            }
            sb.Append("</tbody></table>");
        }
        var isoItems = _verify.Get(pid, "ISO13849-2").ToList();
        var iecItems = _verify.Get(pid, "IEC60204-1").ToList();
        if (isoItems.Count + iecItems.Count > 0)
        {
            sb.Append("<h2>验证清单摘要</h2>");
            if (isoItems.Count > 0)
            {
                sb.Append("<h3>ISO 13849-2</h3><table border='1' cellspacing='0' cellpadding='4'><thead><tr><th>代码</th><th>标题</th><th>条款</th><th>证据</th><th>结果</th><th>责任人</th><th>期限</th></tr></thead><tbody>");
                foreach (var x in isoItems)
                {
                    var ev = string.IsNullOrWhiteSpace(x.EvidenceId) ? "" : (_evidence.Get(x.EvidenceId)?.Name ?? x.EvidenceId);
                    var link = string.IsNullOrWhiteSpace(x.EvidenceId) ? ev : $"<a href='/api/evidence/{x.EvidenceId}/download'>{ev}</a>";
                    sb.Append($"<tr><td>{x.Code}</td><td>{x.Title}</td><td>{x.Clause}</td><td>{link}</td><td>{x.Result}</td><td>{x.Owner}</td><td>{x.Due}</td></tr>");
                }
                sb.Append("</tbody></table>");
            }
            if (iecItems.Count > 0)
            {
                sb.Append("<h3>IEC 60204-1</h3><table border='1' cellspacing='0' cellpadding='4'><thead><tr><th>代码</th><th>标题</th><th>条款</th><th>证据</th><th>结果</th><th>责任人</th><th>期限</th></tr></thead><tbody>");
                foreach (var x in iecItems)
                {
                    var ev = string.IsNullOrWhiteSpace(x.EvidenceId) ? "" : (_evidence.Get(x.EvidenceId)?.Name ?? x.EvidenceId);
                    var link = string.IsNullOrWhiteSpace(x.EvidenceId) ? ev : $"<a href='/api/evidence/{x.EvidenceId}/download'>{ev}</a>";
                    sb.Append($"<tr><td>{x.Code}</td><td>{x.Title}</td><td>{x.Clause}</td><td>{link}</td><td>{x.Result}</td><td>{x.Owner}</td><td>{x.Due}</td></tr>");
                }
                sb.Append("</tbody></table>");
            }
            var byStd = entries.GroupBy(x => x.Standard).ToList();
            sb.Append("<h2 id='clauses'>条款索引（Clause Index）</h2>");
            foreach (var g in byStd)
            {
                var clauses = g.Select(x => x.Clause).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
                sb.Append($"<h3>{g.Key}</h3>");
                sb.Append("<ul>");
                foreach (var cl in clauses) sb.Append($"<li>{cl}</li>");
                sb.Append("</ul>");
            }
        }
        sb.Append("<h2 id='srs'>SRS 摘要（Summary）</h2><p>如项目已创建 SRS，请在系统中联动导出完整 SRS 文档，并确保需求与评估、验证形成双向追溯。核心条款参考：ISO 13849-1（类别、DCavg/MTTFd、CCF≥65、验证），ISO 12100（风险评估与三步法）。</p>");

        sb.Append("</body></html>");
        return sb.ToString();
    }
}

