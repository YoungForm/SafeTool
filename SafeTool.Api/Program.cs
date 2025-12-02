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
builder.Services.AddSingleton<SafeTool.Application.Services.IReportGenerator, SafeTool.Application.Services.HtmlReportGenerator>();
builder.Services.AddSingleton<SafeTool.Application.Services.IPdfReportService, SafeTool.Application.Services.PdfReportService>();
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

app.Run();

public record LoginRequest(string Username, string Password);
