namespace SafeTool.Application.Services;

/// <summary>
/// CI/CD集成服务
/// </summary>
public class CiCdIntegrationService
{
    private readonly BatchEvaluationService _batchEvaluationService;
    private readonly BatchReportService _batchReportService;
    private readonly EvidenceValidationService _evidenceValidationService;

    public CiCdIntegrationService(
        BatchEvaluationService batchEvaluationService,
        BatchReportService batchReportService,
        EvidenceValidationService evidenceValidationService)
    {
        _batchEvaluationService = batchEvaluationService;
        _batchReportService = batchReportService;
        _evidenceValidationService = evidenceValidationService;
    }

    /// <summary>
    /// 执行CI/CD流水线
    /// </summary>
    public async Task<CiCdPipelineResult> ExecutePipelineAsync(CiCdPipelineConfig config)
    {
        var result = new CiCdPipelineResult
        {
            PipelineId = config.PipelineId,
            StartedAt = DateTime.UtcNow,
            Steps = new List<PipelineStep>(),
            Success = false
        };

        try
        {
            // 步骤1：验证证据
            if (config.ValidateEvidence)
            {
                var validationStep = await ExecuteValidationStep(config);
                result.Steps.Add(validationStep);
                if (!validationStep.Success)
                {
                    result.Success = false;
                    result.CompletedAt = DateTime.UtcNow;
                    return result;
                }
            }

            // 步骤2：执行评估
            if (config.RunEvaluations)
            {
                var evaluationStep = await ExecuteEvaluationStep(config);
                result.Steps.Add(evaluationStep);
                if (!evaluationStep.Success)
                {
                    result.Success = false;
                    result.CompletedAt = DateTime.UtcNow;
                    return result;
                }
            }

            // 步骤3：生成报告
            if (config.GenerateReports)
            {
                var reportStep = await ExecuteReportStep(config);
                result.Steps.Add(reportStep);
                if (!reportStep.Success)
                {
                    result.Success = false;
                    result.CompletedAt = DateTime.UtcNow;
                    return result;
                }
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    private async Task<PipelineStep> ExecuteValidationStep(CiCdPipelineConfig config)
    {
        var step = new PipelineStep
        {
            Name = "证据验证",
            StartedAt = DateTime.UtcNow,
            Success = false
        };

        try
        {
            if (config.EvidenceIds != null && config.EvidenceIds.Any())
            {
                var validationResults = await _evidenceValidationService.BatchValidateEvidenceAsync(config.EvidenceIds);
                step.Output = $"验证了 {validationResults.ValidCount} 个证据，{validationResults.InvalidCount} 个无效";
                step.Success = validationResults.InvalidCount == 0;
            }
            else
            {
                step.Output = "跳过证据验证（未指定证据ID）";
                step.Success = true;
            }
        }
        catch (Exception ex)
        {
            step.Error = ex.Message;
            step.Success = false;
        }
        finally
        {
            step.CompletedAt = DateTime.UtcNow;
        }

        return step;
    }

    private async Task<PipelineStep> ExecuteEvaluationStep(CiCdPipelineConfig config)
    {
        var step = new PipelineStep
        {
            Name = "执行评估",
            StartedAt = DateTime.UtcNow,
            Success = false
        };

        try
        {
            if (config.EvaluationRequests != null && config.EvaluationRequests.Any())
            {
                var batchResult = _batchEvaluationService.BatchEvaluateISO13849(config.EvaluationRequests);
                step.Output = $"评估了 {batchResult.EvaluatedCount} 个项目，{batchResult.FailedCount} 个失败";
                step.Success = batchResult.FailedCount == 0;
            }
            else
            {
                step.Output = "跳过评估（未指定评估请求）";
                step.Success = true;
            }
        }
        catch (Exception ex)
        {
            step.Error = ex.Message;
            step.Success = false;
        }
        finally
        {
            step.CompletedAt = DateTime.UtcNow;
        }

        return step;
    }

    private async Task<PipelineStep> ExecuteReportStep(CiCdPipelineConfig config)
    {
        var step = new PipelineStep
        {
            Name = "生成报告",
            StartedAt = DateTime.UtcNow,
            Success = false
        };

        try
        {
            if (config.ReportRequests != null && config.ReportRequests.Any())
            {
                var batchResult = await _batchReportService.GenerateBatchReportsAsync(
                    config.ReportRequests,
                    config.ReportFormat ?? "html",
                    config.ReportLanguage ?? "zh-CN");
                step.Output = $"生成了 {batchResult.GeneratedCount} 个报告，{batchResult.FailedCount} 个失败";
                step.Success = batchResult.FailedCount == 0;
            }
            else
            {
                step.Output = "跳过报告生成（未指定报告请求）";
                step.Success = true;
            }
        }
        catch (Exception ex)
        {
            step.Error = ex.Message;
            step.Success = false;
        }
        finally
        {
            step.CompletedAt = DateTime.UtcNow;
        }

        return step;
    }

    /// <summary>
    /// 生成CI/CD配置模板
    /// </summary>
    public CiCdPipelineConfig GenerateConfigTemplate()
    {
        return new CiCdPipelineConfig
        {
            PipelineId = "default",
            ValidateEvidence = true,
            RunEvaluations = true,
            GenerateReports = true,
            ReportFormat = "html",
            ReportLanguage = "zh-CN"
        };
    }
}

public class CiCdPipelineConfig
{
    public string PipelineId { get; set; } = string.Empty;
    public bool ValidateEvidence { get; set; } = true;
    public bool RunEvaluations { get; set; } = true;
    public bool GenerateReports { get; set; } = true;
    public List<string>? EvidenceIds { get; set; }
    public List<ISO13849EvaluationRequest>? EvaluationRequests { get; set; }
    public List<BatchReportRequest>? ReportRequests { get; set; }
    public string? ReportFormat { get; set; }
    public string? ReportLanguage { get; set; }
}

public class CiCdPipelineResult
{
    public string PipelineId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<PipelineStep> Steps { get; set; } = new();
}

public class PipelineStep
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
}

