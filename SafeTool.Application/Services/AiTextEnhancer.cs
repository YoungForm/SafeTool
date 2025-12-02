using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SafeTool.Domain.Compliance;

namespace SafeTool.Application.Services;

public interface IAiTextEnhancer
{
    Task<string> EnhanceSummaryAsync(ComplianceChecklist input, EvaluationResult result, CancellationToken ct = default);
    Task<string> DraftSrsAsync(SafeTool.Domain.SRS.SrsDocument srs, ComplianceChecklist? checklist, CancellationToken ct = default);
}

public class NullTextEnhancer : IAiTextEnhancer
{
    public Task<string> EnhanceSummaryAsync(ComplianceChecklist input, EvaluationResult result, CancellationToken ct = default)
    {
        var s = result.Summary + "。" + (result.NonConformities.Count == 0 ? "无不符合项" : $"不符合项: {string.Join("；", result.NonConformities)}");
        return Task.FromResult(s);
    }

    public Task<string> DraftSrsAsync(SafeTool.Domain.SRS.SrsDocument srs, ComplianceChecklist? checklist, CancellationToken ct = default)
    {
        var intro = $"系统 {srs.SystemName} 的安全需求规格草案：安全功能 {srs.SafetyFunction}，PLr {srs.RequiredPLr}，类别 {srs.ArchitectureCategory}。";
        var lines = srs.Requirements.Select(r => $"- {r.Title}：{r.Description}（接受准则：{r.AcceptanceCriteria}）");
        var body = string.Join("\n", lines);
        var tail = checklist is null ? "" : $"\n基于当前评估，建议验证达到PL并完善诊断覆盖与CCF措施。";
        return Task.FromResult(intro + "\n" + body + tail);
    }
}

public class OpenAiTextEnhancer : IAiTextEnhancer
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OpenAiTextEnhancer(HttpClient http, string model = "gpt-4o-mini")
    {
        _http = http;
        _model = model;
    }

    public async Task<string> EnhanceSummaryAsync(ComplianceChecklist input, EvaluationResult result, CancellationToken ct = default)
    {
        var payload = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = "你是安全标准专家，精炼中文合规结论，保持客观与可执行建议。" },
                new { role = "user", content = BuildPrompt(input, result) }
            }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return content ?? result.Summary;
    }

    private static string BuildPrompt(ComplianceChecklist c, EvaluationResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("请基于ISO 12100与ISO 13849-1生成简明结论与建议：");
        sb.AppendLine($"系统: {c.SystemName}, 评估人: {c.Assessor}, 日期: {c.AssessmentDate:yyyy-MM-dd}");
        sb.AppendLine($"危害: {string.Join(", ", c.ISO12100.IdentifiedHazards)}; S/F/A: {(int)c.ISO12100.Severity}/{(int)c.ISO12100.Frequency}/{(int)c.ISO12100.Avoidance}");
        sb.AppendLine($"ISO13849: 需求PL {c.ISO13849.RequiredPL}, 架构 {c.ISO13849.Architecture}, DCavg {c.ISO13849.DCavg:P0}, MTTFd {c.ISO13849.MTTFd}, CCF {c.ISO13849.CCFScore}, 验证 {(c.ISO13849.ValidationPerformed ? "是" : "否")}");
        sb.AppendLine($"评估摘要: {r.Summary}");
        if (r.NonConformities.Count > 0) sb.AppendLine($"不符合项: {string.Join("；", r.NonConformities)}");
        sb.AppendLine($"请输出1-2段中文：第一段为合规结论，第二段为具体可执行整改建议。");
        return sb.ToString();
    }

    public async Task<string> DraftSrsAsync(SafeTool.Domain.SRS.SrsDocument srs, ComplianceChecklist? checklist, CancellationToken ct = default)
    {
        var payload = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = "你是功能安全专家，基于ISO 13849-1生成简明SRS草案，条理清晰、无夸大。" },
                new { role = "user", content = BuildSrsPrompt(srs, checklist) }
            }
        };
        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var content = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    private static string BuildSrsPrompt(SafeTool.Domain.SRS.SrsDocument srs, ComplianceChecklist? c)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"系统: {srs.SystemName}; 安全功能: {srs.SafetyFunction}; PLr: {srs.RequiredPLr}; 类别: {srs.ArchitectureCategory}; DCavg: {srs.DCavg:P0}; MTTFd: {srs.MTTFd:0}h; 反应时间: {srs.ReactionTime}; 安全状态: {srs.SafeState}。");
        if (c is not null)
        {
            sb.AppendLine($"评估映射: ISO12100 危害 {string.Join(", ", c.ISO12100.IdentifiedHazards)}; 达成PL {SafeTool.Domain.Standards.ISO13849Calculator.AchievedPL(c.ISO13849)}。");
        }
        sb.AppendLine("请输出结构化草案：关键参数、需求要点（含接受准则）、验证建议（含诊断/CCF），不超过400字。");
        return sb.ToString();
    }
}

