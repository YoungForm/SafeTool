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
}

public class PdfReportService : IPdfReportService
{
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
}
