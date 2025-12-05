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
// 审计签名服务
builder.Services.AddSingleton<SafeTool.Application.Services.AuditSignatureService>(
    sp => new SafeTool.Application.Services.AuditSignatureService(dataDir));
builder.Services.AddSingleton<SafeTool.Application.Services.AuditService>(
    sp => new SafeTool.Application.Services.AuditService(dataDir, sp));
builder.Services.AddSingleton<SafeTool.Application.Services.CcfService>();
builder.Services.AddSingleton(new SafeTool.Application.Services.ComponentLibraryService(dataDir));
builder.Services.AddSingleton(new SafeTool.Application.Services.ComplianceMatrixService(dataDir));
builder.Services.AddSingleton(new SafeTool.Application.Services.EvidenceService(dataDir));
builder.Services.AddSingleton(new SafeTool.Application.Services.VerificationChecklistService(dataDir));
builder.Services.AddSingleton(new SafeTool.Application.Services.ProjectModelService(dataDir));
builder.Services.AddSingleton<SafeTool.Application.Services.ModelComputeService>();
builder.Services.AddSingleton<SafeTool.Application.Services.PlSilMappingService>();

// 变更管理服务（Repository模式）
builder.Services.AddSingleton<SafeTool.Application.Repositories.IChangeRequestRepository>(
    sp => new SafeTool.Application.Repositories.FileBasedChangeRequestRepository(dataDir));
builder.Services.AddSingleton<SafeTool.Application.Services.IChangeImpactAnalyzer, SafeTool.Application.Services.ChangeImpactAnalyzer>();
builder.Services.AddSingleton<SafeTool.Application.Services.IVersionComparer, SafeTool.Application.Services.VersionComparer>();
builder.Services.AddSingleton<SafeTool.Application.Services.ChangeRequestService>();

// 证据到期提醒服务（观察者模式 + 后台任务）
builder.Services.AddSingleton<SafeTool.Application.Services.INotificationService>(
    sp => new SafeTool.Application.Services.NotificationService(dataDir));
builder.Services.AddHostedService<SafeTool.Application.Services.EvidenceExpiryNotificationService>();

// CCF评分向导服务（建造者模式 + 策略模式）
builder.Services.AddSingleton<SafeTool.Application.Services.CcfWizardService>();

// 报告模板服务（模板方法模式）
builder.Services.AddSingleton<SafeTool.Application.Services.ILocalizationService, SafeTool.Application.Services.LocalizationService>();
builder.Services.AddSingleton<SafeTool.Application.Services.IReportTemplateService>(
    sp => new SafeTool.Application.Services.ReportTemplateService(dataDir, sp.GetRequiredService<SafeTool.Application.Services.ILocalizationService>()));

// 合并报告服务
builder.Services.AddSingleton<SafeTool.Application.Services.CombinedReportService>();

// RBAC权限管理服务
builder.Services.AddSingleton<SafeTool.Application.Services.RbacService>(
    sp => new SafeTool.Application.Services.RbacService(dataDir));

// 组件版本管理服务
builder.Services.AddSingleton<SafeTool.Application.Services.ComponentVersionService>(
    sp => new SafeTool.Application.Services.ComponentVersionService(dataDir));

// 组件附件管理服务
builder.Services.AddSingleton<SafeTool.Application.Services.ComponentAttachmentService>(
    sp => new SafeTool.Application.Services.ComponentAttachmentService(dataDir));

// 合规映射矩阵增强服务
builder.Services.AddSingleton<SafeTool.Application.Services.ComplianceMatrixEnhancementService>();

// 数据脱敏服务
builder.Services.AddSingleton<SafeTool.Application.Services.DataMaskingService>();

// 互通格式服务
builder.Services.AddSingleton<SafeTool.Application.Services.InteropFormatService>();

// 电子签名服务
builder.Services.AddSingleton<SafeTool.Application.Services.ElectronicSignatureService>(
    sp => new SafeTool.Application.Services.ElectronicSignatureService(dataDir, sp.GetRequiredService<SafeTool.Application.Services.AuditSignatureService>()));

// SRS任务单联动服务
builder.Services.AddSingleton<SafeTool.Application.Services.SrsTaskLinkageService>();

// 批量报告生成服务
builder.Services.AddSingleton<SafeTool.Application.Services.BatchReportService>();

// IEC 60204-1 电气安全检查服务
builder.Services.AddSingleton<SafeTool.Application.Services.Iec60204ElectricalSafetyService>();

// 故障掩蔽风险分析服务
builder.Services.AddSingleton<SafeTool.Application.Services.FaultMaskingRiskAnalysisService>();

// T1/T10D参数管理服务
builder.Services.AddSingleton<SafeTool.Application.Services.T1T10DManagementService>();

// DCavg计算增强服务
builder.Services.AddSingleton<SafeTool.Application.Services.DcavgCalculationEnhancementService>();

// SISTEMA格式解析器
builder.Services.AddSingleton<SafeTool.Application.Services.SistemaFormatParser>();

// 实时计算反馈服务
builder.Services.AddSingleton<SafeTool.Application.Services.RealTimeCalculationService>();

// 图形化建模服务
builder.Services.AddSingleton<SafeTool.Application.Services.GraphicalModelingService>();

// 组件替代建议服务
builder.Services.AddSingleton<SafeTool.Application.Services.ComponentReplacementService>();

// 双标准并行评估服务
builder.Services.AddSingleton<SafeTool.Application.Services.DualStandardEvaluationService>();

// 联动整改建议服务
builder.Services.AddSingleton<SafeTool.Application.Services.LinkedRemediationService>();

// 方程简化提示服务
builder.Services.AddSingleton<SafeTool.Application.Services.EquationSimplificationService>();

// 规则分层管理服务
builder.Services.AddSingleton<SafeTool.Application.Services.RuleHierarchyService>(
    sp => new SafeTool.Application.Services.RuleHierarchyService(dataDir));

// SRECS结构化分解服务
builder.Services.AddSingleton<SafeTool.Application.Services.SrecsDecompositionService>();

// 组件环境与应用限制服务
builder.Services.AddSingleton<SafeTool.Application.Services.ComponentEnvironmentService>(
    sp => new SafeTool.Application.Services.ComponentEnvironmentService(
        sp.GetRequiredService<SafeTool.Application.Services.ComponentLibraryService>(), dataDir));

// 电气图纸关联服务
builder.Services.AddSingleton<SafeTool.Application.Services.ElectricalDrawingService>(
    sp => new SafeTool.Application.Services.ElectricalDrawingService(dataDir));

// 基线管理服务
builder.Services.AddSingleton<SafeTool.Application.Services.BaselineManagementService>(
    sp => new SafeTool.Application.Services.BaselineManagementService(dataDir));

// 离线/内网部署配置服务
builder.Services.AddSingleton<SafeTool.Application.Services.OfflineDeploymentService>(
    sp => new SafeTool.Application.Services.OfflineDeploymentService(dataDir));

// 系统配置管理服务
builder.Services.AddSingleton<SafeTool.Application.Services.SystemConfigurationService>(
    sp => new SafeTool.Application.Services.SystemConfigurationService(dataDir));

// 统计报表服务
builder.Services.AddSingleton<SafeTool.Application.Services.StatisticsService>();

// 数据导出增强服务
builder.Services.AddSingleton<SafeTool.Application.Services.DataExportEnhancementService>();

// 数据导入增强服务
builder.Services.AddSingleton<SafeTool.Application.Services.DataImportEnhancementService>();

// 工作流引擎服务
builder.Services.AddSingleton<SafeTool.Application.Services.WorkflowEngineService>(
    sp => new SafeTool.Application.Services.WorkflowEngineService(dataDir));

// 性能监控服务
builder.Services.AddSingleton<SafeTool.Application.Services.PerformanceMonitoringService>();

// 缓存管理服务
builder.Services.AddSingleton<SafeTool.Application.Services.CacheManagementService>();

// 项目封面与签审页服务
builder.Services.AddSingleton<SafeTool.Application.Services.ProjectCoverPageService>(
    sp => new SafeTool.Application.Services.ProjectCoverPageService(
        sp.GetRequiredService<SafeTool.Application.Services.ILocalizationService>(), dataDir));

// 通道连接可视化服务
builder.Services.AddSingleton<SafeTool.Application.Services.ChannelVisualizationService>();

// 类别自动推导增强服务
builder.Services.AddSingleton<SafeTool.Application.Services.CategoryDerivationEnhancementService>();

// 本地化增强服务
builder.Services.AddSingleton<SafeTool.Application.Services.LocalizationEnhancementService>(
    sp => new SafeTool.Application.Services.LocalizationEnhancementService(sp.GetRequiredService<SafeTool.Application.Services.ILocalizationService>()));

// 本地化证据包导出服务
builder.Services.AddSingleton<SafeTool.Application.Services.LocalizedEvidencePackageService>();

// 批量评估服务
builder.Services.AddSingleton<SafeTool.Application.Services.BatchEvaluationService>();

// CLI服务
builder.Services.AddSingleton<SafeTool.Application.Services.CliService>();

// CI/CD集成服务
builder.Services.AddSingleton<SafeTool.Application.Services.CiCdIntegrationService>();

// ISO 13849-1 计算增强服务
builder.Services.AddSingleton<SafeTool.Application.Services.Iso13849CalculationEnhancementService>();

// IEC 62061 计算增强服务
builder.Services.AddSingleton<SafeTool.Application.Services.Iec62061CalculationEnhancementService>();

// ISO 13849-2 验证清单增强服务
builder.Services.AddSingleton<SafeTool.Application.Services.Iso13849VerificationEnhancementService>();

// 整改项闭环跟踪服务
builder.Services.AddSingleton<SafeTool.Application.Services.RemediationTrackingService>(
    sp => new SafeTool.Application.Services.RemediationTrackingService(dataDir));

// 证据校验服务
builder.Services.AddSingleton<SafeTool.Application.Services.EvidenceValidationService>();

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
srs.MapPost("/{id}/sign", (SafeTool.Application.Services.SrsService srsService, SafeTool.Application.Services.ElectronicSignatureService signature, string id, SafeTool.Application.Services.CreateSignatureRequest req, HttpRequest request) =>
{
    var doc = srsService.Get(id);
    if (doc is null) return Results.NotFound();
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    var sig = signature.CreateSignature(id, "SRS", user, req.Role ?? "reviewer", req.Comment);
    return Results.Ok(sig);
});
srs.MapGet("/{id}/signatures", (SafeTool.Application.Services.ElectronicSignatureService signature, string id) =>
{
    var signatures = signature.GetDocumentSignatures(id);
    return Results.Ok(signatures);
});
srs.MapPost("/{id}/generate-tasks", (SafeTool.Application.Services.SrsTaskLinkageService linkage, string id, string projectId) =>
{
    var tasks = linkage.GenerateVerificationTasksFromSrs(id, projectId);
    return Results.Ok(tasks);
});
srs.MapGet("/{id}/traceability", (SafeTool.Application.Services.SrsTaskLinkageService linkage, string id, string projectId) =>
{
    var result = linkage.CheckSrsTraceability(id, projectId);
    return Results.Ok(result);
});

srs.MapGet("/audit/logs", (SafeTool.Application.Services.AuditService audit, string? user, string? action, int? skip, int? take) => Results.Ok(audit.Query(user, action, skip ?? 0, take ?? 200)));
srs.MapPost("/audit/verify", (SafeTool.Application.Services.AuditService audit, SafeTool.Application.Services.AuditSignatureService signature, SafeTool.Application.Services.AuditRecord record) =>
{
    var isValid = audit.VerifySignature(record, signature);
    return Results.Ok(new { isValid, message = isValid ? "签名验证通过" : "签名验证失败" });
});
srs.MapGet("/audit/public-key", (SafeTool.Application.Services.AuditSignatureService signature) => Results.Ok(new { publicKey = signature.GetPublicKey() }));

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
iec62061.MapGet("/pl-sil/map", (SafeTool.Application.Services.PlSilMappingService mapping, SafeTool.Domain.Standards.PerformanceLevel? pl, SafeTool.Domain.Standards.SafetyIntegrityLevel? sil) =>
{
    if (pl.HasValue && sil.HasValue)
    {
        var check = SafeTool.Application.Services.PlSilMappingService.CheckConsistency(pl.Value, sil.Value);
        return Results.Ok(check);
    }
    if (pl.HasValue)
    {
        var sils = SafeTool.Application.Services.PlSilMappingService.MapPlToSil(pl.Value);
        var notes = SafeTool.Application.Services.PlSilMappingService.GetMappingNotes(pl: pl.Value);
        return Results.Ok(new { mappedSils = sils, notes });
    }
    if (sil.HasValue)
    {
        var pls = SafeTool.Application.Services.PlSilMappingService.MapSilToPl(sil.Value);
        var notes = SafeTool.Application.Services.PlSilMappingService.GetMappingNotes(sil: sil.Value);
        return Results.Ok(new { mappedPls = pls, notes });
    }
    var generalNotes = SafeTool.Application.Services.PlSilMappingService.GetMappingNotes();
    return Results.Ok(generalNotes);
});

var library = app.MapGroup("/api/library").RequireAuthorization();
library.MapGet("/components", (SafeTool.Application.Services.ComponentLibraryService svc) => Results.Ok(svc.List()));
library.MapGet("/components/{id}", (SafeTool.Application.Services.ComponentLibraryService svc, SafeTool.Application.Services.DataMaskingService masking, SafeTool.Application.Services.RbacService rbac, string id, HttpRequest request) =>
{
    var item = svc.Get(id);
    if (item is null) return Results.NotFound();
    
    // 检查权限，如果没有权限则脱敏敏感参数
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    var hasPermission = rbac.HasPermission(user, "component:view-sensitive");
    var maskedItem = masking.MaskComponentParameters(item, hasPermission);
    
    return Results.Ok(maskedItem);
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
interop.MapGet("/format/specification", (SafeTool.Application.Services.InteropFormatService svc) => Results.Ok(svc.GetFormatSpecification()));
interop.MapGet("/export/sistema/{projectId}", (SafeTool.Application.Services.InteropFormatService svc, string projectId) =>
{
    var csv = svc.ExportToSistemaCsv(projectId);
    return Results.File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", $"sistema_{projectId}.csv");
});
interop.MapPost("/import/sistema", async (SafeTool.Application.Services.InteropFormatService svc, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var csv = await reader.ReadToEndAsync();
    var count = svc.ImportFromSistemaCsv(csv);
    return Results.Ok(new { imported = count });
});
interop.MapGet("/export/pascal/{projectId}", (SafeTool.Application.Services.InteropFormatService svc, string projectId) =>
{
    var json = svc.ExportToPascalJson(projectId);
    return Results.File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"pascal_{projectId}.json");
});
interop.MapPost("/import/pascal", async (SafeTool.Application.Services.InteropFormatService svc, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var json = await reader.ReadToEndAsync();
    var count = svc.ImportFromPascalJson(json);
    return Results.Ok(new { imported = count });
});
interop.MapGet("/export/siemens-set/{projectId}", (SafeTool.Application.Services.InteropFormatService svc, string projectId) =>
{
    var json = svc.ExportToSiemensSetJson(projectId);
    return Results.File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"siemens-set_{projectId}.json");
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

var changeRequest = app.MapGroup("/api/changerequest").RequireAuthorization();
changeRequest.MapPost("", async (SafeTool.Application.Services.ChangeRequestService svc, SafeTool.Domain.ChangeManagement.ChangeRequest cr, HttpRequest request) =>
{
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    var created = await svc.CreateAsync(cr, user);
    return Results.Ok(created);
});
changeRequest.MapPost("/{id}/submit", async (SafeTool.Application.Services.ChangeRequestService svc, string id, HttpRequest request) =>
{
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    var updated = await svc.SubmitAsync(id, user);
    return Results.Ok(updated);
});
changeRequest.MapPost("/{id}/approve", async (SafeTool.Application.Services.ChangeRequestService svc, string id, SafeTool.Application.Services.ApproveRequest req, HttpRequest request) =>
{
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    var updated = await svc.ApproveAsync(id, user, req.Comment ?? "", req.IsFirstReviewer);
    return Results.Ok(updated);
});
changeRequest.MapPost("/{id}/reject", async (SafeTool.Application.Services.ChangeRequestService svc, string id, SafeTool.Application.Services.RejectRequest req, HttpRequest request) =>
{
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    var updated = await svc.RejectAsync(id, user, req.Reason ?? "");
    return Results.Ok(updated);
});
changeRequest.MapPost("/{id}/implement", async (SafeTool.Application.Services.ChangeRequestService svc, string id, HttpRequest request) =>
{
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    var updated = await svc.ImplementAsync(id, user);
    return Results.Ok(updated);
});
changeRequest.MapGet("", async (SafeTool.Application.Services.ChangeRequestService svc, string? projectId, SafeTool.Domain.ChangeManagement.ChangeStatus? status, string? requester) =>
{
    var results = await svc.QueryAsync(projectId, status, requester);
    return Results.Ok(results);
});
changeRequest.MapGet("/{id}", async (SafeTool.Application.Services.ChangeRequestService svc, string id) =>
{
    var repo = app.Services.GetRequiredService<SafeTool.Application.Repositories.IChangeRequestRepository>();
    var cr = await repo.GetByIdAsync(id);
    return cr is null ? Results.NotFound() : Results.Ok(cr);
});
changeRequest.MapPost("/{id}/diff", async (SafeTool.Application.Services.ChangeRequestService svc, string id) =>
{
    var diff = await svc.GenerateVersionDiffAsync(id);
    return Results.Ok(new { diff });
});

var notification = app.MapGroup("/api/notification").RequireAuthorization();
notification.MapGet("", (SafeTool.Application.Services.INotificationService svc, string? userId, SafeTool.Application.Services.NotificationType? type) =>
{
    var results = svc.GetNotificationsAsync(userId, type).Result;
    return Results.Ok(results);
});

var ccfWizard = app.MapGroup("/api/ccf/wizard").RequireAuthorization();
ccfWizard.MapPost("/create", (SafeTool.Application.Services.CcfWizardService svc) => Results.Ok(new { wizardId = Guid.NewGuid().ToString("N") }));
ccfWizard.MapPost("/recommendation", (SafeTool.Application.Services.CcfWizardService svc, SafeTool.Application.Services.CcfRecommendationRequest req) =>
{
    var recommendation = svc.GetRecommendation(req.CurrentScore, req.SelectedCodes ?? Array.Empty<string>());
    return Results.Ok(recommendation);
});

var reportTemplate = app.MapGroup("/api/report/template").RequireAuthorization();
reportTemplate.MapGet("", async (SafeTool.Application.Services.IReportTemplateService svc) => Results.Ok(await svc.ListTemplatesAsync()));
reportTemplate.MapGet("/{id}", async (SafeTool.Application.Services.IReportTemplateService svc, string id) =>
{
    var template = await svc.GetTemplateAsync(id);
    return template is null ? Results.NotFound() : Results.Ok(template);
});
reportTemplate.MapPost("", async (SafeTool.Application.Services.IReportTemplateService svc, SafeTool.Application.Services.ReportTemplate template) =>
{
    var created = await svc.CreateTemplateAsync(template);
    return Results.Ok(created);
});
reportTemplate.MapPut("/{id}", async (SafeTool.Application.Services.IReportTemplateService svc, string id, SafeTool.Application.Services.ReportTemplate template) =>
{
    var updated = await svc.UpdateTemplateAsync(id, template);
    return Results.Ok(updated);
});
reportTemplate.MapDelete("/{id}", async (SafeTool.Application.Services.IReportTemplateService svc, string id) =>
{
    var deleted = await svc.DeleteTemplateAsync(id);
    return deleted ? Results.Ok() : Results.NotFound();
});
reportTemplate.MapPost("/{id}/render", async (SafeTool.Application.Services.IReportTemplateService svc, string id, SafeTool.Application.Services.RenderRequest req) =>
{
    var rendered = await svc.RenderAsync(id, req.Data ?? new { }, req.Language ?? "zh-CN");
    return Results.Text(rendered, "text/html; charset=utf-8");
});

var combinedReport = app.MapGroup("/api/report/combined").RequireAuthorization();
combinedReport.MapPost("", async (
    SafeTool.Application.Services.CombinedReportService svc,
    SafeTool.Domain.Compliance.ComplianceChecklist iso13849Checklist,
    SafeTool.Domain.Compliance.EvaluationResult iso13849Result,
    SafeTool.Domain.Standards.SafetyFunction62061 iec62061Function,
    SafeTool.Application.Services.IEC62061Evaluator iec62061Evaluator,
    string? language) =>
{
    var (iec62061Result, _) = iec62061Evaluator.Evaluate(iec62061Function);
    var html = await svc.GenerateCombinedReportAsync(iso13849Checklist, iso13849Result, iec62061Function, iec62061Result, language ?? "zh-CN");
    return Results.Text(html, "text/html; charset=utf-8");
});

var batchReport = app.MapGroup("/api/report/batch").RequireAuthorization();
batchReport.MapPost("/generate", async (SafeTool.Application.Services.BatchReportService svc, SafeTool.Application.Services.BatchReportRequest[] requests, string? format, string? language) =>
{
    var result = await svc.GenerateBatchReportsAsync(requests, format ?? "html", language ?? "zh-CN");
    return Results.Ok(result);
});
batchReport.MapGet("/cover", (SafeTool.Application.Services.BatchReportService svc, string projectId, string projectName, string? company, string? author, string? language) =>
{
    var html = svc.GenerateProjectCover(projectId, projectName, company, author, language ?? "zh-CN");
    return Results.Text(html, "text/html; charset=utf-8");
});
batchReport.MapPost("/signature-page", (SafeTool.Application.Services.BatchReportService svc, SafeTool.Application.Services.SignatureInfo[] signatures, string? language) =>
{
    var html = svc.GenerateSignaturePage(signatures, language ?? "zh-CN");
    return Results.Text(html, "text/html; charset=utf-8");
});

var signature = app.MapGroup("/api/signature").RequireAuthorization();
signature.MapPost("/verify/{signatureId}", (SafeTool.Application.Services.ElectronicSignatureService svc, string signatureId) =>
{
    var result = svc.VerifySignature(signatureId);
    return Results.Ok(result);
});

var iec60204 = app.MapGroup("/api/iec60204").RequireAuthorization();
iec60204.MapPost("/overload-protection/check", (SafeTool.Application.Services.Iec60204ElectricalSafetyService svc, SafeTool.Application.Services.OverloadProtectionInput input) =>
{
    var result = svc.CheckOverloadProtection(input);
    return Results.Ok(result);
});
iec60204.MapPost("/isolation-short-circuit/check", (SafeTool.Application.Services.Iec60204ElectricalSafetyService svc, SafeTool.Application.Services.IsolationAndShortCircuitInput input) =>
{
    var result = svc.CheckIsolationAndShortCircuit(input);
    return Results.Ok(result);
});
iec60204.MapPost("/comprehensive-check", (SafeTool.Application.Services.Iec60204ElectricalSafetyService svc, SafeTool.Application.Services.OverloadProtectionInput? overload, SafeTool.Application.Services.IsolationAndShortCircuitInput? isolation) =>
{
    var result = svc.ComprehensiveCheck(overload, isolation);
    return Results.Ok(result);
});

var faultMasking = app.MapGroup("/api/fault-masking").RequireAuthorization();
faultMasking.MapPost("/analyze", (SafeTool.Application.Services.FaultMaskingRiskAnalysisService svc, SafeTool.Application.Services.FaultMaskingRiskInput input) =>
{
    var result = svc.AnalyzeRisk(input);
    return Results.Ok(result);
});
faultMasking.MapPost("/report", (SafeTool.Application.Services.FaultMaskingRiskAnalysisService svc, SafeTool.Application.Services.FaultMaskingRiskInput input) =>
{
    var result = svc.AnalyzeRisk(input);
    var report = svc.GenerateRiskReport(result);
    return Results.Text(report, "text/plain; charset=utf-8");
});

var t1t10d = app.MapGroup("/api/t1t10d").RequireAuthorization();
t1t10d.MapPost("/manage", (SafeTool.Application.Services.T1T10DManagementService svc, SafeTool.Application.Services.T1T10DParameters parameters) =>
{
    var result = svc.ManageParameters(parameters);
    return Results.Ok(result);
});
t1t10d.MapPost("/suggest", (SafeTool.Application.Services.T1T10DManagementService svc, double targetSIL, double? currentT10D) =>
{
    var result = svc.SuggestParameters(targetSIL, currentT10D);
    return Results.Ok(result);
});

var dcavg = app.MapGroup("/api/dcavg").RequireAuthorization();
dcavg.MapPost("/calculate-enhanced", (SafeTool.Application.Services.DcavgCalculationEnhancementService svc, SafeTool.Application.Services.EnhancedDcavgInput input) =>
{
    var result = svc.CalculateDcavgEnhanced(input);
    return Results.Ok(result);
});

var sistema = app.MapGroup("/api/sistema").RequireAuthorization();
sistema.MapPost("/parse-library", async (SafeTool.Application.Services.SistemaFormatParser parser, HttpRequest request) =>
{
    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms);
    var fileData = ms.ToArray();
    var fileName = request.Headers["X-File-Name"].ToString();
    var result = parser.ParseSistemaLibrary(fileData, fileName);
    return Results.Ok(result);
});
sistema.MapPost("/parse-project", async (SafeTool.Application.Services.SistemaFormatParser parser, HttpRequest request) =>
{
    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms);
    var fileData = ms.ToArray();
    var fileName = request.Headers["X-File-Name"].ToString();
    var result = parser.ParseSistemaProject(fileData, fileName);
    return Results.Ok(result);
});

var realtime = app.MapGroup("/api/realtime").RequireAuthorization();
realtime.MapPost("/session", (SafeTool.Application.Services.RealTimeCalculationService svc, string? sessionId) =>
{
    var id = svc.CreateSession(sessionId);
    return Results.Ok(new { sessionId = id });
});
realtime.MapPost("/calculate/{sessionId}", async (SafeTool.Application.Services.RealTimeCalculationService svc, string sessionId, SafeTool.Application.Services.CalculationRequest request) =>
{
    var result = await svc.ExecuteCalculationAsync(sessionId, request);
    return Results.Ok(result);
});
realtime.MapGet("/session/{sessionId}", (SafeTool.Application.Services.RealTimeCalculationService svc, string sessionId) =>
{
    var session = svc.GetSession(sessionId);
    return session != null ? Results.Ok(session) : Results.NotFound();
});
realtime.MapPost("/cancel/{sessionId}", (SafeTool.Application.Services.RealTimeCalculationService svc, string sessionId) =>
{
    var cancelled = svc.CancelSession(sessionId);
    return cancelled ? Results.Ok() : Results.NotFound();
});

var graphical = app.MapGroup("/api/graphical").RequireAuthorization();
graphical.MapGet("/model/{functionId}", (SafeTool.Application.Services.GraphicalModelingService svc, string functionId) =>
{
    try
    {
        var result = svc.GetGraphicalModel(functionId);
        return Results.Ok(result);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});
graphical.MapPost("/model/{functionId}", (SafeTool.Application.Services.GraphicalModelingService svc, string functionId, SafeTool.Application.Services.GraphicalModelData modelData) =>
{
    try
    {
        svc.SaveGraphicalModel(functionId, modelData);
        return Results.Ok();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

var visualization = app.MapGroup("/api/visualization").RequireAuthorization();
visualization.MapGet("/channels/{functionId}", (SafeTool.Application.Services.ChannelVisualizationService svc, string functionId) =>
{
    try
    {
        var result = svc.GenerateVisualization(functionId);
        return Results.Ok(result);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

var category = app.MapGroup("/api/category").RequireAuthorization();
category.MapPost("/derive-enhanced", (SafeTool.Application.Services.CategoryDerivationEnhancementService svc, SafeTool.Application.Services.EnhancedCategoryDerivationInput input) =>
{
    var result = svc.DeriveCategory(input);
    return Results.Ok(result);
});

var localization = app.MapGroup("/api/localization").RequireAuthorization();
localization.MapGet("/languages", (SafeTool.Application.Services.LocalizationEnhancementService svc) =>
{
    var languages = svc.GetSupportedLanguages();
    return Results.Ok(languages);
});
localization.MapGet("/strings/{language}", (SafeTool.Application.Services.LocalizationEnhancementService svc, string language) =>
{
    var strings = svc.GetAllLocalizations(language);
    return Results.Ok(strings);
});
localization.MapPost("/format/unit", (SafeTool.Application.Services.LocalizationEnhancementService svc, double value, string unit, string? language) =>
{
    var formatted = svc.FormatUnit(value, unit, language ?? "zh-CN");
    return Results.Ok(new { formatted });
});
localization.MapPost("/format/time", (SafeTool.Application.Services.LocalizationEnhancementService svc, double hours, string? language) =>
{
    var formatted = svc.FormatTimeUnit(hours, language ?? "zh-CN");
    return Results.Ok(new { formatted });
});
localization.MapPost("/format/percentage", (SafeTool.Application.Services.LocalizationEnhancementService svc, double value, string? language) =>
{
    var formatted = svc.FormatPercentage(value, language ?? "zh-CN");
    return Results.Ok(new { formatted });
});
localization.MapPost("/format/datetime", (SafeTool.Application.Services.LocalizationEnhancementService svc, DateTime dateTime, string format, string? language) =>
{
    var formatted = svc.FormatDateTimeLocalized(dateTime, format, language ?? "zh-CN");
    return Results.Ok(new { formatted });
});
localization.MapPost("/format/number", (SafeTool.Application.Services.LocalizationEnhancementService svc, double number, string format, string? language) =>
{
    var formatted = svc.FormatNumberLocalized(number, format, language ?? "zh-CN");
    return Results.Ok(new { formatted });
});

var evidencePackage = app.MapGroup("/api/evidence/package").RequireAuthorization();
evidencePackage.MapPost("/generate", (SafeTool.Application.Services.LocalizedEvidencePackageService svc, string projectId, string? language, string[]? evidenceIds) =>
{
    var package = svc.GeneratePackage(projectId, language ?? "zh-CN", evidenceIds);
    return Results.Ok(package);
});
evidencePackage.MapPost("/export/json", (SafeTool.Application.Services.LocalizedEvidencePackageService svc, SafeTool.Application.Services.LocalizedEvidencePackage package) =>
{
    var json = svc.ExportToJson(package);
    return Results.Text(json, "application/json; charset=utf-8");
});
evidencePackage.MapPost("/export/report", (SafeTool.Application.Services.LocalizedEvidencePackageService svc, SafeTool.Application.Services.LocalizedEvidencePackage package) =>
{
    var html = svc.GeneratePackageReport(package);
    return Results.Text(html, "text/html; charset=utf-8");
});

var batchEvaluation = app.MapGroup("/api/batch/evaluation").RequireAuthorization();
batchEvaluation.MapPost("/iso13849", (SafeTool.Application.Services.BatchEvaluationService svc, SafeTool.Application.Services.ISO13849EvaluationRequest[] requests) =>
{
    var result = svc.BatchEvaluateISO13849(requests);
    return Results.Ok(result);
});
batchEvaluation.MapPost("/iec62061", (SafeTool.Application.Services.BatchEvaluationService svc, SafeTool.Application.Services.IEC62061EvaluationRequest[] requests) =>
{
    var result = svc.BatchEvaluateIEC62061(requests);
    return Results.Ok(result);
});
batchEvaluation.MapPost("/model", (SafeTool.Application.Services.BatchEvaluationService svc, SafeTool.Application.Services.ModelComputationRequest[] requests) =>
{
    var result = svc.BatchComputeModel(requests);
    return Results.Ok(result);
});
batchEvaluation.MapPost("/combined", (SafeTool.Application.Services.BatchEvaluationService svc, SafeTool.Application.Services.ISO13849EvaluationRequest[]? iso13849, SafeTool.Application.Services.IEC62061EvaluationRequest[]? iec62061) =>
{
    var result = svc.CombinedBatchEvaluate(iso13849, iec62061);
    return Results.Ok(result);
});

var cli = app.MapGroup("/api/cli").RequireAuthorization();
cli.MapPost("/execute", async (SafeTool.Application.Services.CliService svc, SafeTool.Application.Services.CliCommand command) =>
{
    var result = await svc.ExecuteCommandAsync(command);
    return Results.Ok(result);
});

var cicd = app.MapGroup("/api/cicd").RequireAuthorization();
cicd.MapPost("/pipeline", async (SafeTool.Application.Services.CiCdIntegrationService svc, SafeTool.Application.Services.CiCdPipelineConfig config) =>
{
    var result = await svc.ExecutePipelineAsync(config);
    return Results.Ok(result);
});
cicd.MapGet("/config/template", (SafeTool.Application.Services.CiCdIntegrationService svc) =>
{
    var template = svc.GenerateConfigTemplate();
    return Results.Ok(template);
});

var rbac = app.MapGroup("/api/rbac").RequireAuthorization();
rbac.MapGet("/roles", (SafeTool.Application.Services.RbacService svc) => Results.Ok(svc.GetRoles()));
rbac.MapGet("/roles/{id}", (SafeTool.Application.Services.RbacService svc, string id) =>
{
    var role = svc.GetRole(id);
    return role is null ? Results.NotFound() : Results.Ok(role);
});
rbac.MapPost("/roles", (SafeTool.Application.Services.RbacService svc, SafeTool.Application.Services.RbacService.Role role) =>
{
    var created = svc.CreateRole(role);
    return Results.Ok(created);
});
rbac.MapGet("/users/{userId}/permissions", (SafeTool.Application.Services.RbacService svc, string userId) =>
{
    var permissions = svc.GetUserPermissions(userId);
    return Results.Ok(permissions);
});
rbac.MapPost("/users/{userId}/roles/{roleId}", (SafeTool.Application.Services.RbacService svc, string userId, string roleId) =>
{
    svc.AssignRole(userId, roleId);
    return Results.Ok();
});
rbac.MapDelete("/users/{userId}/roles/{roleId}", (SafeTool.Application.Services.RbacService svc, string userId, string roleId) =>
{
    svc.RemoveRole(userId, roleId);
    return Results.Ok();
});
rbac.MapPost("/check", (SafeTool.Application.Services.RbacService svc, SafeTool.Application.Services.PermissionCheckRequest req) =>
{
    var hasPermission = svc.HasPermission(req.UserId ?? "", req.Permission ?? "");
    return Results.Ok(new { hasPermission });
});

var componentVersion = app.MapGroup("/api/component/version").RequireAuthorization();
componentVersion.MapGet("/{componentId}", (SafeTool.Application.Services.ComponentVersionService svc, string componentId) =>
{
    var versions = svc.GetVersions(componentId);
    return Results.Ok(versions);
});
componentVersion.MapGet("/{componentId}/{version}", (SafeTool.Application.Services.ComponentVersionService svc, string componentId, string version) =>
{
    var v = svc.GetVersion(componentId, version);
    return v is null ? Results.NotFound() : Results.Ok(v);
});
componentVersion.MapPost("/{componentId}/create", async (
    SafeTool.Application.Services.ComponentVersionService versionSvc,
    SafeTool.Application.Services.ComponentLibraryService libSvc,
    string componentId,
    SafeTool.Application.Services.CreateVersionRequest req,
    HttpRequest request) =>
{
    var component = libSvc.Get(componentId);
    if (component == null)
        return Results.NotFound();
    
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    var version = versionSvc.CreateVersion(componentId, component, user, req.Reason ?? "");
    return Results.Ok(version);
});
componentVersion.MapGet("/{componentId}/compare", (SafeTool.Application.Services.ComponentVersionService svc, string componentId, string version1, string version2) =>
{
    var diff = svc.CompareVersions(componentId, version1, version2);
    return Results.Ok(diff);
});

var componentAttachment = app.MapGroup("/api/component/attachment").RequireAuthorization();
componentAttachment.MapGet("/{componentId}", (SafeTool.Application.Services.ComponentAttachmentService svc, string componentId) =>
{
    var attachments = svc.GetAttachments(componentId);
    return Results.Ok(attachments);
});
componentAttachment.MapPost("/{componentId}", async (SafeTool.Application.Services.ComponentAttachmentService svc, string componentId, HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var name = form["name"].ToString();
    var type = form["type"].ToString();
    var description = form["description"].ToString();
    var file = form.Files.FirstOrDefault();
    
    if (file == null)
        return Results.BadRequest("未提供文件");
    
    var attachment = svc.AddAttachment(componentId, name ?? "", type ?? "", file, description);
    return Results.Ok(attachment);
});
componentAttachment.MapGet("/{componentId}/{attachmentId}/download", (SafeTool.Application.Services.ComponentAttachmentService svc, string componentId, string attachmentId) =>
{
    var file = svc.GetAttachmentFile(componentId, attachmentId);
    return file is null ? Results.NotFound() : Results.File(file.Value.path, file.Value.contentType, fileDownloadName: file.Value.name);
});
componentAttachment.MapDelete("/{componentId}/{attachmentId}", (SafeTool.Application.Services.ComponentAttachmentService svc, string componentId, string attachmentId) =>
{
    var deleted = svc.DeleteAttachment(componentId, attachmentId);
    return deleted ? Results.Ok() : Results.NotFound();
});

var matrixEnhancement = app.MapGroup("/api/compliance/matrix/enhancement").RequireAuthorization();
matrixEnhancement.MapGet("/{projectId}/clause-index", (SafeTool.Application.Services.ComplianceMatrixEnhancementService svc, string projectId, string standard) =>
{
    var index = svc.GetClauseIndex(projectId, standard);
    return Results.Ok(index);
});
matrixEnhancement.MapGet("/{projectId}/check", (SafeTool.Application.Services.ComplianceMatrixEnhancementService svc, string projectId) =>
{
    var result = svc.CheckCompliance(projectId);
    return Results.Ok(result);
});
matrixEnhancement.MapGet("/{projectId}/traceability", (SafeTool.Application.Services.ComplianceMatrixEnhancementService svc, string projectId) =>
{
    var chain = svc.GenerateTraceabilityChain(projectId);
    return Results.Ok(chain);
});

var iso13849Calc = app.MapGroup("/api/iso13849/calculation").RequireAuthorization();
iso13849Calc.MapPost("/dcavg/regular", (SafeTool.Application.Services.Iso13849CalculationEnhancementService svc, SafeTool.Application.Services.DcavgRegularRequest req) =>
{
    var result = svc.CalculateDcavgRegular(req.Devices ?? new List<SafeTool.Application.Services.DeviceDcavgInfo>(), req.DemandRate, req.SeriesCount);
    return Results.Ok(result);
});
iso13849Calc.MapPost("/category/suggest", (SafeTool.Application.Services.Iso13849CalculationEnhancementService svc, SafeTool.Application.Services.CategorySelectionInput input) =>
{
    var result = svc.SuggestCategory(input);
    return Results.Ok(result);
});
iso13849Calc.MapPost("/series/check", (SafeTool.Application.Services.Iso13849CalculationEnhancementService svc, SafeTool.Application.Services.SeriesDeviceCheckRequest req) =>
{
    var result = svc.CheckSeriesDeviceLimit(req.SeriesCount, req.TargetDcavg);
    return Results.Ok(result);
});

var iec62061Calc = app.MapGroup("/api/iec62061/calculation").RequireAuthorization();
iec62061Calc.MapPost("/expiry-risk", (SafeTool.Application.Services.Iec62061CalculationEnhancementService svc, SafeTool.Application.Services.ExpiryRiskCheckRequest req) =>
{
    var result = svc.CheckExpiryRisk(req.T1, req.T10D, req.LastTestDate);
    return Results.Ok(result);
});
iec62061Calc.MapPost("/proof-test-coverage", (SafeTool.Application.Services.Iec62061CalculationEnhancementService svc, SafeTool.Application.Services.ProofTestCoverageCheckRequest req) =>
{
    var result = svc.CheckProofTestCoverage(req.T1, req.T10D, req.Coverage);
    return Results.Ok(result);
});

var iso13849Verify = app.MapGroup("/api/iso13849-2/verification").RequireAuthorization();
iso13849Verify.MapPost("/{projectId}/fault-exclusion", (SafeTool.Application.Services.Iso13849VerificationEnhancementService svc, string projectId) =>
{
    var items = svc.CreateFaultExclusionChecklist(projectId);
    return Results.Ok(items);
});
iso13849Verify.MapPost("/{projectId}/software-requirements", (SafeTool.Application.Services.Iso13849VerificationEnhancementService svc, string projectId) =>
{
    var items = svc.CreateSoftwareRequirementChecklist(projectId);
    return Results.Ok(items);
});
iso13849Verify.MapPost("/{projectId}/verification-plan", (SafeTool.Application.Services.Iso13849VerificationEnhancementService svc, string projectId, SafeTool.Application.Services.CreateVerificationPlanRequest req) =>
{
    var template = svc.CreateVerificationPlanTemplate(projectId, req.SafetyFunctionName ?? "Safety Function");
    return Results.Ok(template);
});

var remediation = app.MapGroup("/api/remediation").RequireAuthorization();
remediation.MapGet("/{projectId}", (SafeTool.Application.Services.RemediationTrackingService svc, string projectId, SafeTool.Application.Services.RemediationStatus? status, string? owner) =>
{
    var items = svc.GetRemediations(projectId, status, owner);
    return Results.Ok(items);
});
remediation.MapGet("/{projectId}/overdue", (SafeTool.Application.Services.RemediationTrackingService svc, string projectId) =>
{
    var items = svc.GetOverdueRemediations(projectId);
    return Results.Ok(items);
});
remediation.MapPost("/{projectId}", (SafeTool.Application.Services.RemediationTrackingService svc, string projectId, SafeTool.Application.Services.RemediationItem item, HttpRequest request) =>
{
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    var created = svc.CreateRemediation(projectId, item);
    return Results.Ok(created);
});
remediation.MapPost("/{projectId}/{itemId}/assign", (SafeTool.Application.Services.RemediationTrackingService svc, string projectId, string itemId, SafeTool.Application.Services.AssignOwnerRequest req, HttpRequest request) =>
{
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    var updated = svc.AssignOwner(projectId, itemId, req.Owner ?? "", user);
    return Results.Ok(updated);
});
remediation.MapPost("/{projectId}/{itemId}/status", (SafeTool.Application.Services.RemediationTrackingService svc, string projectId, string itemId, SafeTool.Application.Services.UpdateStatusRequest req, HttpRequest request) =>
{
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    var updated = svc.UpdateStatus(projectId, itemId, req.Status, user, req.Comment);
    return Results.Ok(updated);
});
remediation.MapPost("/{projectId}/{itemId}/complete", (SafeTool.Application.Services.RemediationTrackingService svc, string projectId, string itemId, SafeTool.Application.Services.CompleteRemediationRequest req, HttpRequest request) =>
{
    var user = request.HttpContext.User?.Identity?.Name ?? "unknown";
    var completed = svc.CompleteRemediation(projectId, itemId, req.EvidenceId, user, req.Comment);
    return Results.Ok(completed);
});

var evidenceValidation = app.MapGroup("/api/evidence/validation").RequireAuthorization();
evidenceValidation.MapPost("/{evidenceId}", (SafeTool.Application.Services.EvidenceValidationService svc, string evidenceId) =>
{
    var result = svc.ValidateEvidence(evidenceId);
    return Results.Ok(result);
});
evidenceValidation.MapPost("/batch", (SafeTool.Application.Services.EvidenceValidationService svc, SafeTool.Application.Services.BatchValidationRequest req) =>
{
    var results = svc.ValidateEvidences(req.EvidenceIds ?? Array.Empty<string>());
    return Results.Ok(results);
});
evidenceValidation.MapPost("/chain/{projectId}", (SafeTool.Application.Services.EvidenceValidationService svc, SafeTool.Application.Services.ComplianceMatrixService matrixSvc, string projectId) =>
{
    var result = svc.ValidateEvidenceChain(projectId, matrixSvc);
    return Results.Ok(result);
});

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

// 组件替代建议服务API
var componentReplacement = app.MapGroup("/api/component/replacement").RequireAuthorization();
componentReplacement.MapGet("/{componentId}", (SafeTool.Application.Services.ComponentReplacementService svc, string componentId, SafeTool.Application.Services.ReplacementCriteria? criteria) =>
{
    var result = svc.GetReplacementSuggestions(componentId, criteria);
    return Results.Ok(result);
});

// 双标准并行评估服务API
var dualStandard = app.MapGroup("/api/dual-standard").RequireAuthorization();
dualStandard.MapPost("/evaluate", (SafeTool.Application.Services.DualStandardEvaluationService svc, SafeTool.Application.Services.DualStandardEvaluationRequest req) =>
{
    var result = svc.Evaluate(req.Iso13849Checklist, req.Iec62061Function);
    return Results.Ok(result);
});

// 联动整改建议服务API
var linkedRemediation = app.MapGroup("/api/remediation/linked").RequireAuthorization();
linkedRemediation.MapPost("/{projectId}", (SafeTool.Application.Services.LinkedRemediationService svc, string projectId, SafeTool.Application.Services.LinkedRemediationRequest req) =>
{
    var result = svc.GenerateLinkedRemediations(projectId, req.CurrentPL, req.CurrentSIL, req.TargetPL, req.TargetSIL);
    return Results.Ok(result);
});

// 方程简化提示服务API
var equationSimplification = app.MapGroup("/api/equation/simplification").RequireAuthorization();
equationSimplification.MapPost("/analyze", (SafeTool.Application.Services.EquationSimplificationService svc, SafeTool.Domain.Standards.SafetyFunction62061 function) =>
{
    var result = svc.AnalyzeAndSimplify(function);
    return Results.Ok(result);
});

// 规则分层管理服务API
var ruleHierarchy = app.MapGroup("/api/rules/hierarchy").RequireAuthorization();
ruleHierarchy.MapGet("/", (SafeTool.Application.Services.RuleHierarchyService svc, string? industry, string? enterprise, string? project) =>
{
    var result = svc.GetRules(industry, enterprise, project);
    return Results.Ok(result);
});
ruleHierarchy.MapPost("/{level}/{levelId}", (SafeTool.Application.Services.RuleHierarchyService svc, string level, string levelId, SafeTool.Application.Services.RuleItem rule) =>
{
    var result = svc.CreateOrUpdateRule(level, levelId, rule);
    return Results.Ok(result);
});
ruleHierarchy.MapDelete("/{level}/{levelId}/{ruleKey}", (SafeTool.Application.Services.RuleHierarchyService svc, string level, string levelId, string ruleKey) =>
{
    var deleted = svc.DeleteRule(level, levelId, ruleKey);
    return deleted ? Results.Ok() : Results.NotFound();
});
ruleHierarchy.MapPost("/compare", (SafeTool.Application.Services.RuleHierarchyService svc, SafeTool.Application.Services.RuleComparisonRequest req) =>
{
    var result = svc.CompareRules(req.Level1, req.LevelId1, req.Level2, req.LevelId2);
    return Results.Ok(result);
});

// SRECS结构化分解服务API
var srecsDecomposition = app.MapGroup("/api/srecs/decomposition").RequireAuthorization();
srecsDecomposition.MapPost("/analyze", (SafeTool.Application.Services.SrecsDecompositionService svc, SafeTool.Domain.Standards.SafetyFunction62061 function) =>
{
    var result = svc.AnalyzeAndDecompose(function);
    return Results.Ok(result);
});

// 组件环境与应用限制服务API
var componentEnvironment = app.MapGroup("/api/component/environment").RequireAuthorization();
componentEnvironment.MapGet("/{componentId}", (SafeTool.Application.Services.ComponentEnvironmentService svc, string componentId) =>
{
    var limits = svc.GetEnvironmentLimits(componentId);
    return limits != null ? Results.Ok(limits) : Results.NotFound();
});
componentEnvironment.MapPost("/{componentId}", (SafeTool.Application.Services.ComponentEnvironmentService svc, string componentId, SafeTool.Application.Services.ComponentEnvironmentLimits limits) =>
{
    var result = svc.SetEnvironmentLimits(componentId, limits);
    return Results.Ok(result);
});
componentEnvironment.MapPost("/{componentId}/validate", (SafeTool.Application.Services.ComponentEnvironmentService svc, string componentId, SafeTool.Application.Services.EnvironmentConditions conditions) =>
{
    var result = svc.ValidateEnvironment(componentId, conditions);
    return Results.Ok(result);
});
componentEnvironment.MapPost("/compatible", (SafeTool.Application.Services.ComponentEnvironmentService svc, SafeTool.Application.Services.EnvironmentConditions conditions) =>
{
    var result = svc.GetCompatibleComponents(conditions);
    return Results.Ok(result);
});

// 电气图纸关联服务API
var electricalDrawing = app.MapGroup("/api/electrical-drawing").RequireAuthorization();
electricalDrawing.MapGet("/{projectId}", (SafeTool.Application.Services.ElectricalDrawingService svc, string projectId, string? resourceType, string? resourceId) =>
{
    var links = svc.GetDrawings(projectId, resourceType, resourceId);
    return Results.Ok(links);
});
electricalDrawing.MapPost("/{projectId}/link", (SafeTool.Application.Services.ElectricalDrawingService svc, string projectId, SafeTool.Application.Services.ElectricalDrawingLinkRequest req) =>
{
    var link = svc.LinkDrawing(projectId, req.ResourceType, req.ResourceId, req.Drawing);
    return Results.Ok(link);
});
electricalDrawing.MapGet("/{projectId}/drawing/{drawingId}/resources", (SafeTool.Application.Services.ElectricalDrawingService svc, string projectId, string drawingId) =>
{
    var links = svc.GetLinkedResources(projectId, drawingId);
    return Results.Ok(links);
});
electricalDrawing.MapDelete("/{projectId}/link/{linkId}", (SafeTool.Application.Services.ElectricalDrawingService svc, string projectId, string linkId) =>
{
    var deleted = svc.UnlinkDrawing(projectId, linkId);
    return deleted ? Results.Ok() : Results.NotFound();
});
electricalDrawing.MapPost("/validate", (SafeTool.Application.Services.ElectricalDrawingService svc, SafeTool.Application.Services.ElectricalDrawingInfo drawing) =>
{
    var result = svc.ValidateDrawing(drawing);
    return Results.Ok(result);
});

// 基线管理服务API
var baseline = app.MapGroup("/api/baseline").RequireAuthorization();
baseline.MapGet("/{projectId}", (SafeTool.Application.Services.BaselineManagementService svc, string projectId) =>
{
    var baselines = svc.GetBaselines(projectId);
    return Results.Ok(baselines);
});
baseline.MapGet("/{projectId}/{baselineId}", (SafeTool.Application.Services.BaselineManagementService svc, string projectId, string baselineId) =>
{
    var baseline = svc.GetBaseline(projectId, baselineId);
    return baseline != null ? Results.Ok(baseline) : Results.NotFound();
});
baseline.MapPost("/{projectId}", (SafeTool.Application.Services.BaselineManagementService svc, string projectId, SafeTool.Application.Services.BaselineInfo info) =>
{
    var baseline = svc.CreateBaseline(projectId, info);
    return Results.Ok(baseline);
});
baseline.MapPost("/{projectId}/current", (SafeTool.Application.Services.BaselineManagementService svc, string projectId, SafeTool.Application.Services.SetCurrentBaselineRequest req) =>
{
    var set = svc.SetCurrentBaseline(projectId, req.BaselineId);
    return set ? Results.Ok() : Results.BadRequest();
});
baseline.MapGet("/{projectId}/current", (SafeTool.Application.Services.BaselineManagementService svc, string projectId) =>
{
    var baseline = svc.GetCurrentBaseline(projectId);
    return baseline != null ? Results.Ok(baseline) : Results.NotFound();
});
baseline.MapPost("/{projectId}/compare", (SafeTool.Application.Services.BaselineManagementService svc, string projectId, SafeTool.Application.Services.CompareBaselinesRequest req) =>
{
    var result = svc.CompareBaselines(projectId, req.BaselineId1, req.BaselineId2);
    return Results.Ok(result);
});
baseline.MapPost("/{projectId}/{baselineId}/restore", (SafeTool.Application.Services.BaselineManagementService svc, string projectId, string baselineId) =>
{
    var restored = svc.RestoreBaseline(projectId, baselineId);
    return restored ? Results.Ok() : Results.BadRequest();
});

// 离线/内网部署配置服务API
var deployment = app.MapGroup("/api/deployment").RequireAuthorization();
deployment.MapPost("/generate", (SafeTool.Application.Services.OfflineDeploymentService svc, SafeTool.Application.Services.DeploymentType type) =>
{
    var config = svc.GenerateConfiguration(type);
    return Results.Ok(config);
});
deployment.MapGet("/{type}", (SafeTool.Application.Services.OfflineDeploymentService svc, SafeTool.Application.Services.DeploymentType type) =>
{
    var config = svc.GetConfiguration(type);
    return config != null ? Results.Ok(config) : Results.NotFound();
});
deployment.MapPost("/validate", (SafeTool.Application.Services.OfflineDeploymentService svc, SafeTool.Application.Services.DeploymentConfiguration config) =>
{
    var result = svc.ValidateConfiguration(config);
    return Results.Ok(result);
});
deployment.MapGet("/{type}/document", (SafeTool.Application.Services.OfflineDeploymentService svc, SafeTool.Application.Services.DeploymentType type) =>
{
    var document = svc.GenerateDeploymentDocument(type);
    return Results.Text(document, "text/markdown");
});

// 系统配置管理服务API
var systemConfig = app.MapGroup("/api/system/config").RequireAuthorization();
systemConfig.MapGet("/", (SafeTool.Application.Services.SystemConfigurationService svc) =>
{
    var config = svc.GetConfiguration();
    return Results.Ok(config);
});
systemConfig.MapPut("/", (SafeTool.Application.Services.SystemConfigurationService svc, SafeTool.Application.Services.SystemConfig config) =>
{
    var updated = svc.UpdateConfiguration(config);
    return Results.Ok(updated);
});
systemConfig.MapGet("/setting/{key}", (SafeTool.Application.Services.SystemConfigurationService svc, string key) =>
{
    var value = svc.GetSetting<object>(key);
    return value != null ? Results.Ok(new { key, value }) : Results.NotFound();
});
systemConfig.MapPost("/setting/{key}", (SafeTool.Application.Services.SystemConfigurationService svc, string key, object value) =>
{
    svc.SetSetting(key, value);
    return Results.Ok();
});
systemConfig.MapPost("/reset", (SafeTool.Application.Services.SystemConfigurationService svc) =>
{
    var config = svc.ResetConfiguration();
    return Results.Ok(config);
});
systemConfig.MapGet("/export", (SafeTool.Application.Services.SystemConfigurationService svc) =>
{
    var json = svc.ExportConfiguration();
    return Results.Text(json, "application/json");
});
systemConfig.MapPost("/import", (SafeTool.Application.Services.SystemConfigurationService svc, SafeTool.Application.Services.ImportConfigRequest req) =>
{
    var config = svc.ImportConfiguration(req.Json);
    return Results.Ok(config);
});

// 统计报表服务API
var statistics = app.MapGroup("/api/statistics").RequireAuthorization();
statistics.MapGet("/system", (SafeTool.Application.Services.StatisticsService svc, string? projectId) =>
{
    var report = svc.GenerateSystemStatistics(projectId);
    return Results.Ok(report);
});

// 数据导出增强服务API
var dataExport = app.MapGroup("/api/export").RequireAuthorization();
dataExport.MapPost("/project/{projectId}", (SafeTool.Application.Services.DataExportEnhancementService svc, string projectId, SafeTool.Application.Services.ExportOptions options) =>
{
    var result = svc.ExportProjectData(projectId, options);
    return Results.Ok(result);
});

// 数据导入增强服务API
var dataImport = app.MapGroup("/api/import").RequireAuthorization();
dataImport.MapPost("/components/json", (SafeTool.Application.Services.DataImportEnhancementService svc, SafeTool.Application.Services.ImportComponentsRequest req) =>
{
    var result = svc.ImportComponents(req.Json, req.Options ?? new SafeTool.Application.Services.ImportOptions());
    return Results.Ok(result);
});
dataImport.MapPost("/components/csv", (SafeTool.Application.Services.DataImportEnhancementService svc, SafeTool.Application.Services.ImportComponentsCsvRequest req) =>
{
    var result = svc.ImportComponentsFromCsv(req.Csv, req.Options ?? new SafeTool.Application.Services.ImportOptions());
    return Results.Ok(result);
});

// 工作流引擎服务API
var workflow = app.MapGroup("/api/workflow").RequireAuthorization();
workflow.MapPost("/", (SafeTool.Application.Services.WorkflowEngineService svc, SafeTool.Application.Services.WorkflowDefinition definition) =>
{
    var result = svc.CreateWorkflow(definition);
    return Results.Ok(result);
});
workflow.MapGet("/{workflowId}", (SafeTool.Application.Services.WorkflowEngineService svc, string workflowId) =>
{
    var workflow = svc.GetWorkflow(workflowId);
    return workflow != null ? Results.Ok(workflow) : Results.NotFound();
});
workflow.MapPost("/{workflowId}/execute", (SafeTool.Application.Services.WorkflowEngineService svc, string workflowId, SafeTool.Application.Services.ExecuteWorkflowRequest req) =>
{
    var result = svc.ExecuteWorkflow(workflowId, req.InputData);
    return Results.Ok(result);
});

// 性能监控服务API
var performance = app.MapGroup("/api/performance").RequireAuthorization();
performance.MapGet("/metrics", (SafeTool.Application.Services.PerformanceMonitoringService svc) =>
{
    var metrics = svc.GetAllMetrics();
    return Results.Ok(metrics);
});
performance.MapGet("/metrics/{operation}", (SafeTool.Application.Services.PerformanceMonitoringService svc, string operation) =>
{
    var metric = svc.GetMetric(operation);
    return metric != null ? Results.Ok(metric) : Results.NotFound();
});
performance.MapGet("/report", (SafeTool.Application.Services.PerformanceMonitoringService svc, DateTime? from, DateTime? to) =>
{
    var report = svc.GenerateReport(from, to);
    return Results.Ok(report);
});
performance.MapGet("/warnings", (SafeTool.Application.Services.PerformanceMonitoringService svc) =>
{
    var warnings = svc.CheckPerformanceWarnings();
    return Results.Ok(warnings);
});
performance.MapPost("/reset", (SafeTool.Application.Services.PerformanceMonitoringService svc, string? operation) =>
{
    svc.ResetMetrics(operation);
    return Results.Ok();
});

// 缓存管理服务API
var cache = app.MapGroup("/api/cache").RequireAuthorization();
cache.MapGet("/statistics", (SafeTool.Application.Services.CacheManagementService svc) =>
{
    var stats = svc.GetStatistics();
    return Results.Ok(stats);
});
cache.MapPost("/clear", (SafeTool.Application.Services.CacheManagementService svc) =>
{
    svc.Clear();
    return Results.Ok();
});
cache.MapDelete("/{key}", (SafeTool.Application.Services.CacheManagementService svc, string key) =>
{
    var removed = svc.Remove(key);
    return removed ? Results.Ok() : Results.NotFound();
});

app.Run();

public record LoginRequest(string Username, string Password);
