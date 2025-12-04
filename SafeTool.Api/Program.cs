using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
var secret = builder.Configuration["AUTH_SECRET"] ?? Environment.GetEnvironmentVariable("AUTH_SECRET") ?? "dev-secret-please-change";
byte[] key;
if (secret.StartsWith("base64:", StringComparison.OrdinalIgnoreCase))
    key = Convert.FromBase64String(secret.Substring(7));
else
    key = System.Text.Encoding.UTF8.GetBytes(secret);
if (key.Length < 32)
    key = System.Security.Cryptography.SHA256.HashData(key);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});
builder.Services.AddSingleton<SafeTool.Application.Services.ComplianceEvaluator>();
builder.Services.AddSingleton<SafeTool.Application.Services.IEC62061Evaluator>();
builder.Services.AddSingleton<SafeTool.Application.Services.IReportGenerator, SafeTool.Application.Services.HtmlReportGenerator>();
builder.Services.AddSingleton<SafeTool.Application.Services.IPdfReportService, SafeTool.Application.Services.PdfReportService>();
builder.Services.AddSingleton<SafeTool.Application.Services.IIec62061ReportGenerator, SafeTool.Application.Services.Iec62061HtmlReportGenerator>();
builder.Services.AddSingleton<SafeTool.Application.Services.InteropService>();
// AI增强：如果提供OPENAI_API_KEY则使用OpenAI，否则使用本地摘要
var apiKey = builder.Configuration["OPENAI_API_KEY"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (!string.IsNullOrWhiteSpace(apiKey))
{
    builder.Services.AddHttpClient<SafeTool.Application.Services.OpenAiTextEnhancer>(c =>
    {
        c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        c.Timeout = TimeSpan.FromSeconds(20);
    });
    builder.Services.AddSingleton<SafeTool.Application.Services.IAiTextEnhancer>(sp => sp.GetRequiredService<SafeTool.Application.Services.OpenAiTextEnhancer>());
}
else
{
    builder.Services.AddSingleton<SafeTool.Application.Services.IAiTextEnhancer, SafeTool.Application.Services.NullTextEnhancer>();
}
builder.Services.AddSingleton<SafeTool.Application.Services.SrsService>();
builder.Services.AddSingleton<SafeTool.Application.Services.SrsTraceService>();
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "AppData");
Directory.CreateDirectory(dataDir);
builder.Services.AddSingleton(new SafeTool.Application.Services.PlrRuleService(dataDir));
builder.Services.AddSingleton(new SafeTool.Application.Services.AuditService(dataDir));
builder.Services.AddSingleton<SafeTool.Application.Services.CcfService>();
builder.Services.AddSingleton(new SafeTool.Application.Services.ComponentLibraryService(dataDir));
builder.Services.AddSingleton(new SafeTool.Application.Services.ComplianceMatrixService(dataDir));
builder.Services.AddSingleton(new SafeTool.Application.Services.EvidenceService(dataDir));
builder.Services.AddSingleton(new SafeTool.Application.Services.VerificationChecklistService(dataDir));
builder.Services.AddSingleton(new SafeTool.Application.Services.ProjectModelService(dataDir));
builder.Services.AddSingleton<SafeTool.Application.Services.ModelComputeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapPost("/api/auth/login", (LoginRequest req, SafeTool.Application.Services.AuditService audit) =>
{
    // 简化的演示：仅进行空密码检查并返回固定令牌
    if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { message = "用户名或密码不能为空" });
    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var descriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
    {
        Subject = new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, req.Username),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "user")
        }),
        Expires = DateTime.UtcNow.AddHours(8),
        SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256)
    };
    var token = handler.CreateToken(descriptor);
    var jwt = handler.WriteToken(token);
    audit.Log(req.Username, "login", "auth", "成功登录并发放JWT");
    return Results.Ok(new { token = jwt, user = req.Username });
});

var compliance = app.MapGroup("/api/compliance").RequireAuthorization();
compliance.MapPost("/evaluate", (SafeTool.Application.Services.ComplianceEvaluator evaluator, SafeTool.Domain.Compliance.ComplianceChecklist checklist, HttpRequest request) =>
{
    var result = evaluator.Evaluate(checklist);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "evaluate", "compliance", $"系统 {checklist.SystemName} 评估完成，合规: {result.IsCompliant}");
    return Results.Ok(result);
});

compliance.MapPost("/plr", (SafeTool.Application.Services.PlrRuleService rules, SafeTool.Domain.Standards.SeverityLevel s, SafeTool.Domain.Standards.FrequencyLevel f, SafeTool.Domain.Standards.AvoidanceLevel a) =>
{
    var plr = rules.EvaluateRequiredPlr(s, f, a);
    return Results.Ok(new { riskLevel = SafeTool.Domain.Standards.ISO12100Risk.RiskLevel(SafeTool.Domain.Standards.ISO12100Risk.RiskScore(s, f, a)), requiredPLr = plr });
});

compliance.MapGet("/plr/rules", (SafeTool.Application.Services.PlrRuleService rules) => Results.Ok(rules.GetRules()));
compliance.MapPost("/plr/rules", (SafeTool.Application.Services.PlrRuleService rules, Dictionary<string,string> map) => { rules.SetRules(map); return Results.Ok(rules.GetRules()); });

compliance.MapPost("/report", async (SafeTool.Application.Services.ComplianceEvaluator evaluator, SafeTool.Application.Services.IReportGenerator reports, SafeTool.Application.Services.IAiTextEnhancer ai, SafeTool.Domain.Compliance.ComplianceChecklist checklist, HttpRequest request) =>
{
    var result = evaluator.Evaluate(checklist);
    // 使用AI增强摘要
    result.Summary = await ai.EnhanceSummaryAsync(checklist, result, request.HttpContext.RequestAborted);
    var html = reports.GenerateHtml(checklist, result);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "report-html", "compliance", $"生成HTML报告: {checklist.SystemName}");
    return Results.Text(html, "text/html; charset=utf-8");
});

compliance.MapPost("/report.pdf", async (SafeTool.Application.Services.ComplianceEvaluator evaluator, SafeTool.Application.Services.IPdfReportService pdf, SafeTool.Application.Services.IAiTextEnhancer ai, SafeTool.Domain.Compliance.ComplianceChecklist checklist, HttpRequest request) =>
{
    var result = evaluator.Evaluate(checklist);
    result.Summary = await ai.EnhanceSummaryAsync(checklist, result, request.HttpContext.RequestAborted);
    var bytes = pdf.GenerateCompliancePdf(checklist, result);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "report-pdf", "compliance", $"生成PDF报告: {checklist.SystemName}");
    return Results.File(bytes, "application/pdf", fileDownloadName: "ComplianceReport.pdf");
});

var srs = app.MapGroup("/api/srs").RequireAuthorization();
var iso13849 = app.MapGroup("/api/iso13849").RequireAuthorization();
iso13849.MapGet("/ccf/items", (SafeTool.Application.Services.CcfService ccf) => Results.Ok(ccf.GetItems()));
iso13849.MapPost("/ccf/score", (SafeTool.Application.Services.CcfService ccf, IEnumerable<string> codes) => Results.Ok(new { score = ccf.ComputeScore(codes) }));
srs.MapPost("/create", (SafeTool.Application.Services.SrsService service, SafeTool.Domain.SRS.SrsDocument doc, HttpRequest request) =>
{
    var created = service.Create(doc);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "create", "srs", $"创建SRS: {created.Id}");
    return Results.Ok(created);
});

srs.MapGet("/{id}", (SafeTool.Application.Services.SrsService service, string id, HttpRequest request) =>
{
    var doc = service.Get(id);
    return doc is null ? Results.NotFound() : Results.Ok(doc);
});

srs.MapPost("/{id}/approve", (SafeTool.Application.Services.SrsService service, string id, HttpRequest request) =>
{
    var ok = service.Approve(id);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "approve", "srs", ok ? $"审批SRS: {id}" : $"审批失败: {id}");
    return ok ? Results.Ok() : Results.NotFound();
});

srs.MapGet("/{id}/export", (SafeTool.Application.Services.SrsService service, string id, HttpRequest request) =>
{
    var doc = service.Get(id);
    if (doc is null) return Results.NotFound();
    var html = service.ExportHtml(doc);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "export-html", "srs", $"导出SRS HTML: {id}");
    return Results.Text(html, "text/html; charset=utf-8");
});

srs.MapGet("/{id}/export.pdf", (SafeTool.Application.Services.SrsService service, SafeTool.Application.Services.IPdfReportService pdf, string id, HttpRequest request) =>
{
    var doc = service.Get(id);
    if (doc is null) return Results.NotFound();
    var bytes = pdf.GenerateSrsPdf(doc);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "export-pdf", "srs", $"导出SRS PDF: {id}");
    return Results.File(bytes, "application/pdf", fileDownloadName: "SRS.pdf");
});

srs.MapPost("/{id}/trace", (SafeTool.Application.Services.SrsService srsService, SafeTool.Application.Services.SrsTraceService trace, string id, SafeTool.Domain.Compliance.ComplianceChecklist? checklist, HttpRequest request) =>
{
    var doc = srsService.Get(id);
    if (doc is null) return Results.NotFound();
    var issues = trace.CheckConsistency(doc, checklist);
    return Results.Ok(issues);
});

srs.MapPost("/{id}/draft", async (SafeTool.Application.Services.SrsService srsService, SafeTool.Application.Services.IAiTextEnhancer ai, string id, SafeTool.Domain.Compliance.ComplianceChecklist? checklist, HttpRequest request) =>
{
    var doc = srsService.Get(id);
    if (doc is null) return Results.NotFound();
    var text = await ai.DraftSrsAsync(doc, checklist, request.HttpContext.RequestAborted);
    return Results.Text(text, "text/plain; charset=utf-8");
});

srs.MapGet("/audit/logs", (SafeTool.Application.Services.AuditService audit, string? user, string? action, int? skip, int? take) => Results.Ok(audit.Query(user, action, skip ?? 0, take ?? 200)));

var iec62061 = app.MapGroup("/api/iec62061").RequireAuthorization();
iec62061.MapPost("/evaluate", (SafeTool.Application.Services.IEC62061Evaluator eval, SafeTool.Domain.Standards.SafetyFunction62061 func, HttpRequest request) =>
{
    var (result, input) = eval.Evaluate(func);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "evaluate", "iec62061", $"评估 {input.Name} PFHd={result.PFHd:E2} SIL={result.AchievedSIL}");
    return Results.Ok(result);
});
iec62061.MapPost("/report", (SafeTool.Application.Services.IEC62061Evaluator eval, SafeTool.Application.Services.IIec62061ReportGenerator reports, SafeTool.Domain.Standards.SafetyFunction62061 func, HttpRequest request) =>
{
    var (result, input) = eval.Evaluate(func);
    var html = reports.GenerateHtml(input, result);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "report-html", "iec62061", $"生成IEC62061 HTML报告: {input.Name}");
    return Results.Text(html, "text/html; charset=utf-8");
});
iec62061.MapPost("/report.pdf", (SafeTool.Application.Services.IEC62061Evaluator eval, SafeTool.Application.Services.IPdfReportService pdf, SafeTool.Domain.Standards.SafetyFunction62061 func, HttpRequest request) =>
{
    var (result, input) = eval.Evaluate(func);
    var bytes = pdf.GenerateIec62061Pdf(input, result);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "report-pdf", "iec62061", $"生成IEC62061 PDF报告: {input.Name}");
    return Results.File(bytes, "application/pdf", fileDownloadName: "IEC62061Report.pdf");
});

var library = app.MapGroup("/api/library").RequireAuthorization();
library.MapGet("/components", (SafeTool.Application.Services.ComponentLibraryService svc) => Results.Ok(svc.List()));
library.MapGet("/components/{id}", (SafeTool.Application.Services.ComponentLibraryService svc, string id) =>
{
    var item = svc.Get(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
});
library.MapPost("/components", (SafeTool.Application.Services.ComponentLibraryService svc, SafeTool.Application.Services.ComponentLibraryService.ComponentRecord rec, HttpRequest request) =>
{
    var added = svc.Add(rec);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "add", "library", $"新增组件 {added.Id}");
    return Results.Ok(added);
});
library.MapPut("/components/{id}", (SafeTool.Application.Services.ComponentLibraryService svc, string id, SafeTool.Application.Services.ComponentLibraryService.ComponentRecord rec) =>
{
    return svc.Update(id, rec) ? Results.Ok() : Results.NotFound();
});
library.MapDelete("/components/{id}", (SafeTool.Application.Services.ComponentLibraryService svc, string id) =>
{
    return svc.Delete(id) ? Results.Ok() : Results.NotFound();
});
library.MapPost("/import", async (SafeTool.Application.Services.ComponentLibraryService svc, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var json = await reader.ReadToEndAsync();
    var count = svc.ImportJson(json);
    return Results.Ok(new { imported = count });
});
library.MapGet("/export", (SafeTool.Application.Services.ComponentLibraryService svc) => Results.Text(svc.ExportJson(), "application/json"));

var interop = app.MapGroup("/api/interop").RequireAuthorization();
interop.MapPost("/import", async (SafeTool.Application.Services.InteropService svc, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var json = await reader.ReadToEndAsync();
    var dto = svc.ImportJson(json);
    return Results.Ok(dto);
});
interop.MapPost("/export", (SafeTool.Application.Services.InteropService svc, SafeTool.Domain.Interop.ProjectDto dto, string? target, SafeTool.Application.Services.ProjectModelService modelSvc) =>
{
    if (string.IsNullOrWhiteSpace(target) || target!.Equals("json", StringComparison.OrdinalIgnoreCase))
        return Results.Text(svc.ExportJson(dto), "application/json");
    if (target!.Equals("project", StringComparison.OrdinalIgnoreCase))
        return Results.Text(modelSvc.ExportJson(), "application/json");
    var obj = svc.ExportTarget(dto, target!);
    return Results.Ok(obj);
});

var model = app.MapGroup("/api/model").RequireAuthorization();
model.MapGet("/project", (SafeTool.Application.Services.ProjectModelService svc) => Results.Text(svc.ExportJson(), "application/json"));
model.MapPost("/project", async (SafeTool.Application.Services.ProjectModelService svc, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var json = await reader.ReadToEndAsync();
    var n = svc.ImportJson(json);
    return Results.Ok(new { functions = n });
});
model.MapGet("/functions", (SafeTool.Application.Services.ProjectModelService svc) => Results.Ok(svc.List()));
model.MapPost("/functions", (SafeTool.Application.Services.ProjectModelService svc, SafeTool.Application.Services.ProjectModelService.Function f, HttpRequest request) =>
{
    var saved = svc.Upsert(f);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "upsert", "model", $"函数 {saved.Id} {saved.Name}");
    return Results.Ok(saved);
});
model.MapPost("/compute", (SafeTool.Application.Services.ModelComputeService compute, SafeTool.Application.Services.ProjectModelService.Function f) =>
{
    var r = compute.Compute(f);
    return Results.Ok(r);
});

var matrix = app.MapGroup("/api/compliance/matrix").RequireAuthorization();
matrix.MapGet("", (SafeTool.Application.Services.ComplianceMatrixService svc, string projectId) => Results.Ok(svc.Get(projectId)));
matrix.MapPost("", (SafeTool.Application.Services.ComplianceMatrixService svc, string projectId, SafeTool.Application.Services.ComplianceMatrixService.Entry entry, HttpRequest request) =>
{
    var added = svc.Add(projectId, entry);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "add", "matrix", $"项目 {projectId} 新增矩阵条目 {added.Id}");
    return Results.Ok(added);
});
matrix.MapPost("/export", (SafeTool.Application.Services.ComplianceMatrixService svc, string projectId) =>
{
    var csv = svc.ExportCsv(projectId);
    return Results.Text(csv, "text/csv; charset=utf-8");
});
matrix.MapPost("/import", async (SafeTool.Application.Services.ComplianceMatrixService svc, string projectId, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var csv = await reader.ReadToEndAsync();
    var n = svc.ImportCsv(projectId, csv);
    return Results.Ok(new { imported = n });
});

var evidence = app.MapGroup("/api/evidence").RequireAuthorization();
evidence.MapGet("", (SafeTool.Application.Services.EvidenceService svc, string? type, string? status) => Results.Ok(svc.List(type, status)));
evidence.MapPost("", async (SafeTool.Application.Services.EvidenceService svc, HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var name = form["name"].ToString();
    var type = form["type"].ToString();
    var note = form["note"].ToString();
    var source = form["source"].ToString();
    var issuer = form["issuer"].ToString();
    DateTime? validUntil = null; if (DateTime.TryParse(form["validUntil"].ToString(), out var du)) validUntil = du;
    var url = form["url"].ToString();
    var file = form.Files.FirstOrDefault();
    var e = svc.Add(name, type, note, file, source, issuer, validUntil, url);
    return Results.Ok(e);
});
evidence.MapPost("/link", (SafeTool.Application.Services.EvidenceService svc, string evidenceId, string resourceType, string resourceId) =>
{
    var l = svc.CreateLink(evidenceId, resourceType, resourceId);
    return Results.Ok(l);
});
evidence.MapGet("/{id}", (SafeTool.Application.Services.EvidenceService svc, string id) =>
{
    var e = svc.Get(id);
    return e is null ? Results.NotFound() : Results.Ok(e);
});
evidence.MapGet("/{id}/download", (SafeTool.Application.Services.EvidenceService svc, string id) =>
{
    var f = svc.GetFile(id);
    return f is null ? Results.NotFound() : Results.File(f.Value.path, f.Value.contentType, fileDownloadName: f.Value.name);
});

var verification = app.MapGroup("/api/verification").RequireAuthorization();
verification.MapGet("/items", (SafeTool.Application.Services.VerificationChecklistService svc, string projectId, string standard) => Results.Ok(svc.Get(projectId, standard)));
verification.MapPost("/items", (SafeTool.Application.Services.VerificationChecklistService svc, string projectId, string standard, SafeTool.Application.Services.VerificationChecklistService.Item item, HttpRequest request) =>
{
    var saved = svc.Upsert(projectId, standard, item);
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    app.Services.GetRequiredService<SafeTool.Application.Services.AuditService>().Log(user, "upsert", "verification", $"{projectId}/{standard} 条目 {saved.Code}={saved.Result}");
    return Results.Ok(saved);
});
verification.MapPost("/seed", (SafeTool.Application.Services.VerificationChecklistService svc, string projectId, string standard) => Results.Ok(svc.Seed(projectId, standard)));

library.MapGet("/export.csv", (SafeTool.Application.Services.ComponentLibraryService svc) =>
{
    var items = svc.List();
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("id,manufacturer,model,category,PFHd,beta");
    foreach (var x in items)
    {
        var pf = x.Parameters?.GetValueOrDefault("PFHd") ?? x.Parameters?.GetValueOrDefault("pfhd") ?? "";
        var b = x.Parameters?.GetValueOrDefault("beta") ?? x.Parameters?.GetValueOrDefault("Beta") ?? "";
        sb.AppendLine($"{x.Id},{x.Manufacturer},{x.Model},{x.Category},{pf},{b}");
    }
    return Results.Text(sb.ToString(), "text/csv; charset=utf-8");
});

app.Run();

public record LoginRequest(string Username, string Password);
