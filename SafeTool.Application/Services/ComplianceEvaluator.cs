using SafeTool.Domain.Compliance;
using SafeTool.Domain.Standards;

namespace SafeTool.Application.Services;

public class ComplianceEvaluator
{
    public SafeTool.Domain.Compliance.EvaluationResult Evaluate(ComplianceChecklist data)
    {
        var details = new Dictionary<string, string>();

        var score = ISO12100Risk.RiskScore(data.ISO12100.Severity, data.ISO12100.Frequency, data.ISO12100.Avoidance);
        var level = ISO12100Risk.RiskLevel(score);
        details["ISO12100.RiskScore"] = score.ToString();
        details["ISO12100.RiskLevel"] = level;

        var achievedPl = ISO13849Calculator.AchievedPL(data.ISO13849);
        details["ISO13849.AchievedPL"] = achievedPl.ToString();
        details["ISO13849.RequiredPL"] = data.ISO13849.RequiredPL.ToString();

        var nonConformities = new List<string>();
        if (level is "High" or "Extreme" && string.IsNullOrWhiteSpace(data.ISO12100.RiskReductionMeasures))
            nonConformities.Add("ISO12100: 高风险未提供风险降低措施");

        if (!ISO13849Calculator.MeetsRequirement(data.ISO13849))
            nonConformities.Add("ISO13849-1: 未满足所需PL或验证/CCF不足");

        foreach (var item in data.GeneralItems.Where(i => i.Required && !i.Completed))
            nonConformities.Add($"一般项未完成: {item.Title} ({item.Code})");

        var isCompliant = nonConformities.Count == 0;
        var summary = isCompliant
            ? "系统自检符合 ISO 12100 与 ISO 13849-1 要求"
            : "系统自检存在不符合项，需整改";

        var recommended = isCompliant ? "无" : BuildRecommendations(level, data);

        return new EvaluationResult
        {
            IsCompliant = isCompliant,
            Summary = summary,
            Details = details,
            NonConformities = nonConformities,
            RecommendedActions = recommended
        };
    }

    private static string BuildRecommendations(string riskLevel, ComplianceChecklist data)
    {
        var parts = new List<string>();
        if (riskLevel is "High" or "Extreme")
            parts.Add("实施固有安全设计、增加防护与信息防护，并复核S/F/A参数");

        var a = data.ISO13849;
        parts.Add($"验证PL: 目标 {a.RequiredPL}, 当前 {ISO13849Calculator.AchievedPL(a)}; 提升架构或DC/MTTFd，确保CCF≥65并完成验证");

        foreach (var item in data.GeneralItems.Where(i => i.Required && !i.Completed))
            parts.Add($"完成一般项: {item.Title}");

        return string.Join("；", parts);
    }
}

