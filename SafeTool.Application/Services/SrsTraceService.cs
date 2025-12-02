using SafeTool.Domain.Compliance;
using SafeTool.Domain.SRS;

namespace SafeTool.Application.Services;

public class SrsTraceService
{
    public record TraceIssue(string Type, string Message);

    public IEnumerable<TraceIssue> CheckConsistency(SrsDocument srs, ComplianceChecklist? checklist)
    {
        var issues = new List<TraceIssue>();

        if (string.IsNullOrWhiteSpace(srs.RequiredPLr))
            issues.Add(new("SRS", "缺少所需 PLr"));

        if (string.IsNullOrWhiteSpace(srs.ArchitectureCategory))
            issues.Add(new("SRS", "缺少架构类别（B/1/2/3/4）"));

        if (srs.DCavg <= 0)
            issues.Add(new("SRS", "DCavg 未设置"));

        if (srs.MTTFd <= 0)
            issues.Add(new("SRS", "MTTFd 未设置"));

        if (string.IsNullOrWhiteSpace(srs.SafeState))
            issues.Add(new("SRS", "未定义安全状态"));

        if (checklist is not null)
        {
            var achieved = SafeTool.Domain.Standards.ISO13849Calculator.AchievedPL(checklist.ISO13849);
            if (!SafeTool.Domain.Standards.ISO13849Calculator.MeetsRequirement(checklist.ISO13849))
                issues.Add(new("ISO13849", $"达到 PL {achieved} 未满足所需 {checklist.ISO13849.RequiredPL} 或验证/CCF不足"));

            var score = SafeTool.Domain.Standards.ISO12100Risk.RiskScore(checklist.ISO12100.Severity, checklist.ISO12100.Frequency, checklist.ISO12100.Avoidance);
            var level = SafeTool.Domain.Standards.ISO12100Risk.RiskLevel(score);
            if ((level == "High" || level == "Extreme") && string.IsNullOrWhiteSpace(checklist.ISO12100.RiskReductionMeasures))
                issues.Add(new("ISO12100", "高风险未提供风险降低措施"));
        }

        foreach (var r in srs.Requirements.Where(r => r.Mandatory && string.IsNullOrWhiteSpace(r.AcceptanceCriteria)))
            issues.Add(new("SRS", $"需求 {r.Title} 缺少接受准则"));

        return issues;
    }
}

