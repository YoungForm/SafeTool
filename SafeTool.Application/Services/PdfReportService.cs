using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SafeTool.Domain.Compliance;
using SafeTool.Domain.SRS;
using SafeTool.Domain.Standards;

namespace SafeTool.Application.Services;

public interface IPdfReportService
{
    byte[] GenerateCompliancePdf(ComplianceChecklist c, EvaluationResult r);
    byte[] GenerateSrsPdf(SrsDocument srs);
    byte[] GenerateIec62061Pdf(SafeTool.Domain.Standards.SafetyFunction62061 f, SafeTool.Domain.Standards.IEC62061EvaluationResult r);
}

public class PdfReportService : IPdfReportService
{
    private readonly ComplianceMatrixService _matrix;
    private readonly EvidenceService _evidence;
    private readonly VerificationChecklistService _verify;
    public PdfReportService(ComplianceMatrixService matrix, EvidenceService evidence, VerificationChecklistService verify) { _matrix = matrix; _evidence = evidence; _verify = verify; }
    public byte[] GenerateCompliancePdf(ComplianceChecklist c, EvaluationResult r)
    {
        var score = ISO12100Risk.RiskScore(c.ISO12100.Severity, c.ISO12100.Frequency, c.ISO12100.Avoidance);
        var level = ISO12100Risk.RiskLevel(score);
        var achieved = ISO13849Calculator.AchievedPL(c.ISO13849);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Text($"合规自检报告 - {c.SystemName}").SemiBold().FontSize(18);
                page.Content().Column(col =>
                {
                    col.Item().Text($"评估人: {c.Assessor}    日期: {c.AssessmentDate:yyyy-MM-dd}");
                    col.Item().Text(r.Summary);
                    col.Item().Text("ISO 12100").SemiBold();
                    col.Item().Text($"风险评分: {score}  等级: {level}");
                    if (!string.IsNullOrWhiteSpace(c.ISO12100.RiskReductionMeasures))
                        col.Item().Text($"风险降低措施: {c.ISO12100.RiskReductionMeasures}");
                    col.Item().Text("ISO 13849-1").SemiBold();
                    col.Item().Text($"所需PL: {c.ISO13849.RequiredPL}; 达到PL: {achieved}; 架构: {c.ISO13849.Architecture}; DCavg: {c.ISO13849.DCavg:P0}; MTTFd: {c.ISO13849.MTTFd:0}h; CCF: {c.ISO13849.CCFScore}; 验证: {(c.ISO13849.ValidationPerformed ? "是" : "否")}");
                    if (r.NonConformities.Count > 0)
                    {
                        col.Item().Text("不符合项:").SemiBold();
                        foreach (var n in r.NonConformities)
                            col.Item().Text("• " + n);
                        col.Item().Text($"整改建议: {r.RecommendedActions}");
                    }
                    var pid = string.IsNullOrWhiteSpace(c.ProjectId) ? c.SystemName : c.ProjectId;
                    var entries = _matrix.Get(pid).ToList();
                    if (entries.Count > 0)
                    {
                        col.Item().Text("合规矩阵摘要").SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Text("标准");
                                header.Cell().Text("条款");
                                header.Cell().Text("要求");
                                header.Cell().Text("引用");
                                header.Cell().Text("证据");
                                header.Cell().Text("结果");
                                header.Cell().Text("责任人");
                                header.Cell().Text("期限");
                            });
                            foreach (var x in entries)
                            {
                                var ev = string.IsNullOrWhiteSpace(x.EvidenceId) ? "" : (_evidence.Get(x.EvidenceId)?.Name ?? x.EvidenceId);
                                table.Cell().Text(x.Standard);
                                table.Cell().Text(x.Clause);
                                table.Cell().Text(x.Requirement);
                                table.Cell().Text(x.Reference);
                                table.Cell().Text(ev);
                                table.Cell().Text(x.Result);
                                table.Cell().Text(x.Owner ?? "");
                                table.Cell().Text(x.Due ?? "");
                            }
                        });
                    }
                });
                page.Footer().AlignRight().Text("生成于 " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"));
            });
            container.Page(page =>
            {
                page.Margin(20);
                page.Content().Column(col =>
                {
                    col.Item().Text("签审页").SemiBold().FontSize(16);
                    col.Item().Text("编制（Prepared by）: ");
                    col.Item().Text("复核（Reviewed by）: ");
                    col.Item().Text("批准（Approved by）: ");
                    col.Item().Text("日期（Date）: ");
                });
            });
            container.Page(page =>
            {
                page.Margin(20);
                page.Content().Column(col =>
                {
                    var isoItems = _verify.Get(c.ProjectId ?? c.SystemName, "ISO13849-2").ToList();
                    var iecItems = _verify.Get(c.ProjectId ?? c.SystemName, "IEC60204-1").ToList();
                    if (isoItems.Count + iecItems.Count > 0)
                    {
                        col.Item().Text("验证清单摘要").SemiBold().FontSize(16);
                        if (isoItems.Count > 0)
                        {
                            col.Item().Text("ISO 13849-2").SemiBold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns => { columns.RelativeColumn(1); columns.RelativeColumn(2); columns.RelativeColumn(1); columns.RelativeColumn(2); columns.RelativeColumn(1); columns.RelativeColumn(1); });
                                table.Header(h => { h.Cell().Text("代码"); h.Cell().Text("标题"); h.Cell().Text("条款"); h.Cell().Text("证据"); h.Cell().Text("结果"); h.Cell().Text("期限"); });
                                foreach (var x in isoItems)
                                {
                                    var ev = string.IsNullOrWhiteSpace(x.EvidenceId) ? "" : (_evidence.Get(x.EvidenceId)?.Name ?? x.EvidenceId);
                                    table.Cell().Text(x.Code);
                                    table.Cell().Text(x.Title);
                                    table.Cell().Text(x.Clause);
                                    table.Cell().Text(ev);
                                    table.Cell().Text(x.Result);
                                    table.Cell().Text(x.Due ?? "");
                                }
                            });
                        }
                        if (iecItems.Count > 0)
                        {
                            col.Item().Text("IEC 60204-1").SemiBold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns => { columns.RelativeColumn(1); columns.RelativeColumn(2); columns.RelativeColumn(1); columns.RelativeColumn(2); columns.RelativeColumn(1); columns.RelativeColumn(1); });
                                table.Header(h => { h.Cell().Text("代码"); h.Cell().Text("标题"); h.Cell().Text("条款"); h.Cell().Text("证据"); h.Cell().Text("结果"); h.Cell().Text("期限"); });
                                foreach (var x in iecItems)
                                {
                                    var ev = string.IsNullOrWhiteSpace(x.EvidenceId) ? "" : (_evidence.Get(x.EvidenceId)?.Name ?? x.EvidenceId);
                                    table.Cell().Text(x.Code);
                                    table.Cell().Text(x.Title);
                                    table.Cell().Text(x.Clause);
                                    table.Cell().Text(ev);
                                    table.Cell().Text(x.Result);
                                    table.Cell().Text(x.Due ?? "");
                                }
                            });
                        }
                    }
                });
            });
            container.Page(page =>
            {
                page.Margin(20);
                page.Content().Column(col =>
                {
                    var pid = string.IsNullOrWhiteSpace(c.ProjectId) ? c.SystemName : c.ProjectId;
                    var entries = _matrix.Get(pid).ToList();
                    if (entries.Count > 0)
                    {
                        col.Item().Text("条款索引（Clause Index）").SemiBold().FontSize(16);
                        var byStd = entries.GroupBy(x => x.Standard).ToList();
                        foreach (var g in byStd)
                        {
                            col.Item().Text(g.Key).SemiBold();
                            var clauses = g.Select(x => x.Clause).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
                            foreach (var cl in clauses)
                                col.Item().Text("• " + cl);
                        }
                    }
                });
            });
        });
        return doc.GeneratePdf();
    }

    public byte[] GenerateSrsPdf(SrsDocument d)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Text($"SRS - {d.SystemName} ({d.Version})").SemiBold().FontSize(18);
                page.Content().Column(col =>
                {
                    col.Item().Text($"状态: {d.Status}    创建: {d.CreatedAt:yyyy-MM-dd}");
                    col.Item().Text("关键参数").SemiBold();
                    col.Item().Text($"安全功能: {d.SafetyFunction}; PLr: {d.RequiredPLr}; 类别: {d.ArchitectureCategory}; DCavg: {d.DCavg:P0}; MTTFd: {d.MTTFd:0}h");
                    col.Item().Text($"反应时间: {d.ReactionTime}; 安全状态: {d.SafeState}");
                    col.Item().Text($"诊断策略: {d.DiagnosticsStrategy}; I/O: {d.IOMap}");
                    col.Item().Text($"环境: {d.EnvironmentalRequirements}; EMC: {d.EMCRequirements}");
                    col.Item().Text($"维护与测试: {d.MaintenanceTesting}; CCF 措施: {d.CCFMeasures}");
                    col.Item().Text("需求列表").SemiBold();
                    foreach (var r in d.Requirements)
                        col.Item().Text($"• {r.Title}（{r.Category}，{(r.Mandatory ? "必需" : "可选")}）; 接受准则: {r.AcceptanceCriteria}; 条款: {r.ClauseRef}");
                });
                page.Footer().AlignRight().Text("生成于 " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"));
            });
            container.Page(page =>
            {
                page.Margin(20);
                page.Content().Column(col =>
                {
                    col.Item().Text("签审页").SemiBold().FontSize(16);
                    col.Item().Text("编制（Prepared by）: ");
                    col.Item().Text("复核（Reviewed by）: ");
                    col.Item().Text("批准（Approved by）: ");
                    col.Item().Text("日期（Date）: ");
                });
            });
        });
        return doc.GeneratePdf();
    }

    public byte[] GenerateIec62061Pdf(SafeTool.Domain.Standards.SafetyFunction62061 f, SafeTool.Domain.Standards.IEC62061EvaluationResult r)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Text($"IEC 62061 评估报告 - {f.Name}").SemiBold().FontSize(18);
                page.Content().Column(col =>
                {
                    col.Item().Text($"目标SIL: {f.TargetSIL}");
                    col.Item().Text($"PFHd: {r.PFHd:E2}    达到SIL: {r.AchievedSIL}");
                    if (f.ProofTestIntervalT1.HasValue || f.MissionTimeT10D.HasValue)
                        col.Item().Text($"T1: {f.ProofTestIntervalT1?.ToString() ?? "-"}；T10D: {f.MissionTimeT10D?.ToString() ?? "-"}");
                    foreach (var s in f.Subsystems)
                    {
                        col.Item().Text($"子系统: {s.Name}（{s.Architecture}）").SemiBold();
                        foreach (var c in s.Components)
                            col.Item().Text($"• {c.Manufacturer} {c.Model} — PFHd: {c.PFHd:E2}；β: {(c.Beta?.ToString() ?? "-")}");
                    }
                    if (r.Warnings.Count > 0)
                    {
                        col.Item().Text("提示").SemiBold();
                        foreach (var w in r.Warnings) col.Item().Text("• " + w);
                    }
                    var entries = _matrix.Get(f.Id).ToList();
                    if (entries.Count > 0)
                    {
                        col.Item().Text("合规矩阵摘要").SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Text("标准");
                                header.Cell().Text("条款");
                                header.Cell().Text("要求");
                                header.Cell().Text("引用");
                                header.Cell().Text("证据ID");
                                header.Cell().Text("结果");
                                header.Cell().Text("责任人");
                                header.Cell().Text("期限");
                            });
                            foreach (var x in entries)
                            {
                                table.Cell().Text(x.Standard);
                                table.Cell().Text(x.Clause);
                                table.Cell().Text(x.Requirement);
                                table.Cell().Text(x.Reference);
                                table.Cell().Text(x.EvidenceId ?? "");
                                table.Cell().Text(x.Result);
                                table.Cell().Text(x.Owner ?? "");
                                table.Cell().Text(x.Due ?? "");
                            }
                        });
                    }
                });
                page.Footer().AlignRight().Text("生成于 " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"));
            });
        });
        return doc.GeneratePdf();
    }
}
