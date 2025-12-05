namespace SafeTool.Application.Services;

/// <summary>
/// 批量评估服务（工厂模式）
/// </summary>
public class BatchEvaluationService
{
    private readonly ComplianceEvaluator _complianceEvaluator;
    private readonly IEC62061Evaluator _iec62061Evaluator;
    private readonly ModelComputeService _modelComputeService;

    public BatchEvaluationService(
        ComplianceEvaluator complianceEvaluator,
        IEC62061Evaluator iec62061Evaluator,
        ModelComputeService modelComputeService)
    {
        _complianceEvaluator = complianceEvaluator;
        _iec62061Evaluator = iec62061Evaluator;
        _modelComputeService = modelComputeService;
    }

    /// <summary>
    /// 批量执行ISO 13849-1评估
    /// </summary>
    public BatchEvaluationResult BatchEvaluateISO13849(IEnumerable<ISO13849EvaluationRequest> requests)
    {
        var result = new BatchEvaluationResult
        {
            TotalCount = requests.Count(),
            EvaluatedCount = 0,
            FailedCount = 0,
            Evaluations = new List<EvaluationResult>(),
            Errors = new List<string>()
        };

        foreach (var request in requests)
        {
            try
            {
                var evaluation = _complianceEvaluator.Evaluate(request.Checklist);
                result.Evaluations.Add(new EvaluationResult
                {
                    ProjectId = request.ProjectId,
                    Standard = "ISO13849-1",
                    Result = evaluation,
                    EvaluatedAt = DateTime.UtcNow
                });
                result.EvaluatedCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add($"项目 {request.ProjectId} 评估失败: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// 批量执行IEC 62061评估
    /// </summary>
    public BatchEvaluationResult BatchEvaluateIEC62061(IEnumerable<IEC62061EvaluationRequest> requests)
    {
        var result = new BatchEvaluationResult
        {
            TotalCount = requests.Count(),
            EvaluatedCount = 0,
            FailedCount = 0,
            Evaluations = new List<EvaluationResult>(),
            Errors = new List<string>()
        };

        foreach (var request in requests)
        {
            try
            {
                var (evaluationResult, _) = _iec62061Evaluator.Evaluate(request.Function);
                result.Evaluations.Add(new EvaluationResult
                {
                    ProjectId = request.ProjectId,
                    Standard = "IEC62061",
                    Result = evaluationResult,
                    EvaluatedAt = DateTime.UtcNow
                });
                result.EvaluatedCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add($"项目 {request.ProjectId} 评估失败: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// 批量执行安全功能建模计算
    /// </summary>
    public BatchComputationResult BatchComputeModel(IEnumerable<ModelComputationRequest> requests)
    {
        var result = new BatchComputationResult
        {
            TotalCount = requests.Count(),
            ComputedCount = 0,
            FailedCount = 0,
            Computations = new List<ComputationResult>(),
            Errors = new List<string>()
        };

        foreach (var request in requests)
        {
            try
            {
                var computation = _modelComputeService.Compute(request.Function);
                result.Computations.Add(new ComputationResult
                {
                    ProjectId = request.ProjectId,
                    FunctionId = request.Function.Id,
                    Result = computation,
                    ComputedAt = DateTime.UtcNow
                });
                result.ComputedCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add($"项目 {request.ProjectId} 功能 {request.Function.Id} 计算失败: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// 综合批量评估（ISO 13849-1 + IEC 62061）
    /// </summary>
    public CombinedBatchEvaluationResult CombinedBatchEvaluate(
        IEnumerable<ISO13849EvaluationRequest>? iso13849Requests = null,
        IEnumerable<IEC62061EvaluationRequest>? iec62061Requests = null)
    {
        var result = new CombinedBatchEvaluationResult
        {
            ISO13849Result = iso13849Requests != null
                ? BatchEvaluateISO13849(iso13849Requests)
                : null,
            IEC62061Result = iec62061Requests != null
                ? BatchEvaluateIEC62061(iec62061Requests)
                : null
        };

        result.TotalCount = (result.ISO13849Result?.TotalCount ?? 0) + (result.IEC62061Result?.TotalCount ?? 0);
        result.TotalEvaluated = (result.ISO13849Result?.EvaluatedCount ?? 0) + (result.IEC62061Result?.EvaluatedCount ?? 0);
        result.TotalFailed = (result.ISO13849Result?.FailedCount ?? 0) + (result.IEC62061Result?.FailedCount ?? 0);

        return result;
    }
}

public class ISO13849EvaluationRequest
{
    public string ProjectId { get; set; } = string.Empty;
    public SafeTool.Domain.Compliance.ComplianceChecklist Checklist { get; set; } = new();
}

public class IEC62061EvaluationRequest
{
    public string ProjectId { get; set; } = string.Empty;
    public SafeTool.Domain.Standards.SafetyFunction62061 Function { get; set; } = new();
}

public class ModelComputationRequest
{
    public string ProjectId { get; set; } = string.Empty;
    public ProjectModelService.Function Function { get; set; } = new();
}

public class BatchEvaluationResult
{
    public int TotalCount { get; set; }
    public int EvaluatedCount { get; set; }
    public int FailedCount { get; set; }
    public List<EvaluationResult> Evaluations { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class EvaluationResult
{
    public string ProjectId { get; set; } = string.Empty;
    public string Standard { get; set; } = string.Empty;
    public object Result { get; set; } = new();
    public DateTime EvaluatedAt { get; set; }
}

public class BatchComputationResult
{
    public int TotalCount { get; set; }
    public int ComputedCount { get; set; }
    public int FailedCount { get; set; }
    public List<ComputationResult> Computations { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class ComputationResult
{
    public string ProjectId { get; set; } = string.Empty;
    public string FunctionId { get; set; } = string.Empty;
    public object Result { get; set; } = new();
    public DateTime ComputedAt { get; set; }
}

public class CombinedBatchEvaluationResult
{
    public BatchEvaluationResult? ISO13849Result { get; set; }
    public BatchEvaluationResult? IEC62061Result { get; set; }
    public int TotalCount { get; set; }
    public int TotalEvaluated { get; set; }
    public int TotalFailed { get; set; }
}

