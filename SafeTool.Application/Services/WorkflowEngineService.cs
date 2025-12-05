using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 工作流引擎服务（P2优先级）
/// 管理工作流定义和执行
/// </summary>
public class WorkflowEngineService
{
    private readonly string _dataDir;

    public WorkflowEngineService(string dataDir)
    {
        _dataDir = dataDir;
        EnsureDirectories();
    }

    /// <summary>
    /// 创建工作流定义
    /// </summary>
    public WorkflowDefinition CreateWorkflow(WorkflowDefinition definition)
    {
        definition.Id = definition.Id ?? Guid.NewGuid().ToString();
        definition.CreatedAt = DateTime.UtcNow;
        definition.UpdatedAt = DateTime.UtcNow;

        SaveWorkflow(definition);
        return definition;
    }

    /// <summary>
    /// 获取工作流定义
    /// </summary>
    public WorkflowDefinition? GetWorkflow(string workflowId)
    {
        var path = GetWorkflowPath(workflowId);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<WorkflowDefinition>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 执行工作流
    /// </summary>
    public WorkflowExecutionResult ExecuteWorkflow(
        string workflowId,
        Dictionary<string, object>? inputData = null)
    {
        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
        {
            return new WorkflowExecutionResult
            {
                Success = false,
                Error = "工作流不存在"
            };
        }

        var execution = new WorkflowExecution
        {
            Id = Guid.NewGuid().ToString(),
            WorkflowId = workflowId,
            StartedAt = DateTime.UtcNow,
            Status = WorkflowExecutionStatus.Running,
            InputData = inputData ?? new Dictionary<string, object>(),
            Steps = new List<WorkflowStepExecution>()
        };

        try
        {
            // 执行工作流步骤
            foreach (var step in workflow.Steps)
            {
                var stepExecution = ExecuteStep(step, execution);
                execution.Steps.Add(stepExecution);

                if (stepExecution.Status == WorkflowStepStatus.Failed)
                {
                    execution.Status = WorkflowExecutionStatus.Failed;
                    execution.Error = stepExecution.Error;
                    break;
                }
            }

            if (execution.Status == WorkflowExecutionStatus.Running)
            {
                execution.Status = WorkflowExecutionStatus.Completed;
            }

            execution.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            execution.Status = WorkflowExecutionStatus.Failed;
            execution.Error = ex.Message;
            execution.CompletedAt = DateTime.UtcNow;
        }

        SaveExecution(execution);

        return new WorkflowExecutionResult
        {
            ExecutionId = execution.Id,
            Success = execution.Status == WorkflowExecutionStatus.Completed,
            Status = execution.Status.ToString(),
            Error = execution.Error,
            OutputData = execution.OutputData,
            Steps = execution.Steps.Select(s => new StepResult
            {
                StepId = s.StepId,
                Status = s.Status.ToString(),
                Output = s.Output
            }).ToList()
        };
    }

    /// <summary>
    /// 执行工作流步骤
    /// </summary>
    private WorkflowStepExecution ExecuteStep(
        WorkflowStep step,
        WorkflowExecution execution)
    {
        var stepExecution = new WorkflowStepExecution
        {
            StepId = step.Id,
            StepName = step.Name,
            StartedAt = DateTime.UtcNow,
            Status = WorkflowStepStatus.Running
        };

        try
        {
            // 检查条件
            if (!EvaluateCondition(step.Condition, execution))
            {
                stepExecution.Status = WorkflowStepStatus.Skipped;
                stepExecution.Output = "条件不满足，跳过步骤";
                stepExecution.CompletedAt = DateTime.UtcNow;
                return stepExecution;
            }

            // 执行动作
            stepExecution.Output = ExecuteAction(step.Action, execution);
            stepExecution.Status = WorkflowStepStatus.Completed;
            stepExecution.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            stepExecution.Status = WorkflowStepStatus.Failed;
            stepExecution.Error = ex.Message;
            stepExecution.CompletedAt = DateTime.UtcNow;
        }

        return stepExecution;
    }

    /// <summary>
    /// 评估条件
    /// </summary>
    private bool EvaluateCondition(string? condition, WorkflowExecution execution)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return true;

        // 简单的条件评估（实际应使用表达式引擎）
        // 这里只是示例实现
        return true;
    }

    /// <summary>
    /// 执行动作
    /// </summary>
    private string ExecuteAction(WorkflowAction action, WorkflowExecution execution)
    {
        return action.Type switch
        {
            "Notify" => $"通知: {action.Parameters?.GetValueOrDefault("Message") ?? ""}",
            "Validate" => $"验证: {action.Parameters?.GetValueOrDefault("Data") ?? ""}",
            "Transform" => $"转换: {action.Parameters?.GetValueOrDefault("Data") ?? ""}",
            _ => "未知动作类型"
        };
    }

    /// <summary>
    /// 保存工作流
    /// </summary>
    private void SaveWorkflow(WorkflowDefinition workflow)
    {
        var path = GetWorkflowPath(workflow.Id!);
        var json = JsonSerializer.Serialize(workflow, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 保存执行记录
    /// </summary>
    private void SaveExecution(WorkflowExecution execution)
    {
        var dir = Path.Combine(_dataDir, "workflows", "executions", execution.WorkflowId);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{execution.Id}.json");
        var json = JsonSerializer.Serialize(execution, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 获取工作流路径
    /// </summary>
    private string GetWorkflowPath(string workflowId)
    {
        var dir = Path.Combine(_dataDir, "workflows", "definitions");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{workflowId}.json");
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private void EnsureDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_dataDir, "workflows", "definitions"));
        Directory.CreateDirectory(Path.Combine(_dataDir, "workflows", "executions"));
    }
}

public class WorkflowDefinition
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public List<WorkflowStep> Steps { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class WorkflowStep
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Condition { get; set; }
    public WorkflowAction Action { get; set; } = new();
    public int Order { get; set; }
}

public class WorkflowAction
{
    public string Type { get; set; } = string.Empty; // Notify/Validate/Transform
    public Dictionary<string, object>? Parameters { get; set; }
}

public class WorkflowExecution
{
    public string Id { get; set; } = string.Empty;
    public string WorkflowId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public WorkflowExecutionStatus Status { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> InputData { get; set; } = new();
    public Dictionary<string, object> OutputData { get; set; } = new();
    public List<WorkflowStepExecution> Steps { get; set; } = new();
}

public enum WorkflowExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public class WorkflowStepExecution
{
    public string StepId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public WorkflowStepStatus Status { get; set; }
    public string? Error { get; set; }
    public string? Output { get; set; }
}

public enum WorkflowStepStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}

public class WorkflowExecutionResult
{
    public string ExecutionId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
    public Dictionary<string, object>? OutputData { get; set; }
    public List<StepResult> Steps { get; set; } = new();
}

public class StepResult
{
    public string StepId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Output { get; set; }
}

