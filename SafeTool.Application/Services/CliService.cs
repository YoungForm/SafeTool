using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// CLI服务（命令行接口支持）
/// </summary>
public class CliService
{
    private readonly ComplianceEvaluator _complianceEvaluator;
    private readonly IEC62061Evaluator _iec62061Evaluator;
    private readonly BatchEvaluationService _batchEvaluationService;
    private readonly BatchReportService _batchReportService;

    public CliService(
        ComplianceEvaluator complianceEvaluator,
        IEC62061Evaluator iec62061Evaluator,
        BatchEvaluationService batchEvaluationService,
        BatchReportService batchReportService)
    {
        _complianceEvaluator = complianceEvaluator;
        _iec62061Evaluator = iec62061Evaluator;
        _batchEvaluationService = batchEvaluationService;
        _batchReportService = batchReportService;
    }

    /// <summary>
    /// 执行CLI命令
    /// </summary>
    public async Task<CliCommandResult> ExecuteCommandAsync(CliCommand command)
    {
        var result = new CliCommandResult
        {
            Command = command.Command,
            Success = false,
            Output = string.Empty,
            Errors = new List<string>()
        };

        try
        {
            switch (command.Command.ToLower())
            {
                case "evaluate":
                    result = await ExecuteEvaluateCommand(command);
                    break;
                case "batch-evaluate":
                    result = await ExecuteBatchEvaluateCommand(command);
                    break;
                case "generate-report":
                    result = await ExecuteGenerateReportCommand(command);
                    break;
                case "batch-report":
                    result = await ExecuteBatchReportCommand(command);
                    break;
                case "export":
                    result = await ExecuteExportCommand(command);
                    break;
                case "import":
                    result = await ExecuteImportCommand(command);
                    break;
                default:
                    result.Errors.Add($"未知命令: {command.Command}");
                    result.Output = GetHelpText();
                    break;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"执行命令时发生错误: {ex.Message}");
        }

        return result;
    }

    private async Task<CliCommandResult> ExecuteEvaluateCommand(CliCommand command)
    {
        var result = new CliCommandResult
        {
            Command = command.Command,
            Success = false,
            Output = string.Empty,
            Errors = new List<string>()
        };

        if (command.Arguments == null || !command.Arguments.ContainsKey("standard"))
        {
            result.Errors.Add("缺少必需参数: standard");
            return result;
        }

        var standard = command.Arguments["standard"].ToString();
        if (standard == "ISO13849-1")
        {
            // ISO 13849-1评估
            if (!command.Arguments.ContainsKey("checklist"))
            {
                result.Errors.Add("缺少必需参数: checklist");
                return result;
            }

            var checklistJson = command.Arguments["checklist"].ToString();
            var checklist = JsonSerializer.Deserialize<SafeTool.Domain.Compliance.ComplianceChecklist>(
                checklistJson!,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (checklist == null)
            {
                result.Errors.Add("无效的checklist JSON");
                return result;
            }

            var evaluation = _complianceEvaluator.Evaluate(checklist);
            result.Output = JsonSerializer.Serialize(evaluation, new JsonSerializerOptions { WriteIndented = true });
            result.Success = true;
        }
        else if (standard == "IEC62061")
        {
            // IEC 62061评估
            if (!command.Arguments.ContainsKey("function"))
            {
                result.Errors.Add("缺少必需参数: function");
                return result;
            }

            var functionJson = command.Arguments["function"].ToString();
            var function = JsonSerializer.Deserialize<SafeTool.Domain.Standards.SafetyFunction62061>(
                functionJson!,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (function == null)
            {
                result.Errors.Add("无效的function JSON");
                return result;
            }

            var (evaluationResult, _) = _iec62061Evaluator.Evaluate(function);
            result.Output = JsonSerializer.Serialize(evaluationResult, new JsonSerializerOptions { WriteIndented = true });
            result.Success = true;
        }
        else
        {
            result.Errors.Add($"不支持的评估标准: {standard}");
        }

        return result;
    }

    private async Task<CliCommandResult> ExecuteBatchEvaluateCommand(CliCommand command)
    {
        var result = new CliCommandResult
        {
            Command = command.Command,
            Success = false,
            Output = string.Empty,
            Errors = new List<string>()
        };

        if (command.Arguments == null || !command.Arguments.ContainsKey("requests"))
        {
            result.Errors.Add("缺少必需参数: requests");
            return result;
        }

        var requestsJson = command.Arguments["requests"].ToString();
        var requests = JsonSerializer.Deserialize<List<ISO13849EvaluationRequest>>(
            requestsJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (requests == null)
        {
            result.Errors.Add("无效的requests JSON");
            return result;
        }

        var batchResult = _batchEvaluationService.BatchEvaluateISO13849(requests);
        result.Output = JsonSerializer.Serialize(batchResult, new JsonSerializerOptions { WriteIndented = true });
        result.Success = true;

        return result;
    }

    private async Task<CliCommandResult> ExecuteGenerateReportCommand(CliCommand command)
    {
        var result = new CliCommandResult
        {
            Command = command.Command,
            Success = false,
            Output = "报告生成功能需要更多参数",
            Errors = new List<string>()
        };

        // 报告生成逻辑
        result.Success = true;
        return result;
    }

    private async Task<CliCommandResult> ExecuteBatchReportCommand(CliCommand command)
    {
        var result = new CliCommandResult
        {
            Command = command.Command,
            Success = false,
            Output = string.Empty,
            Errors = new List<string>()
        };

        if (command.Arguments == null || !command.Arguments.ContainsKey("requests"))
        {
            result.Errors.Add("缺少必需参数: requests");
            return result;
        }

        var requestsJson = command.Arguments["requests"].ToString();
        var requests = JsonSerializer.Deserialize<BatchReportRequest[]>(
            requestsJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (requests == null)
        {
            result.Errors.Add("无效的requests JSON");
            return result;
        }

        var format = command.Arguments.ContainsKey("format")
            ? command.Arguments["format"].ToString() ?? "html"
            : "html";
        var language = command.Arguments.ContainsKey("language")
            ? command.Arguments["language"].ToString() ?? "zh-CN"
            : "zh-CN";

        var batchResult = await _batchReportService.GenerateBatchReportsAsync(requests, format, language);
        result.Output = JsonSerializer.Serialize(batchResult, new JsonSerializerOptions { WriteIndented = true });
        result.Success = true;

        return result;
    }

    private async Task<CliCommandResult> ExecuteExportCommand(CliCommand command)
    {
        var result = new CliCommandResult
        {
            Command = command.Command,
            Success = false,
            Output = "导出功能需要更多参数",
            Errors = new List<string>()
        };

        // 导出逻辑
        result.Success = true;
        return result;
    }

    private async Task<CliCommandResult> ExecuteImportCommand(CliCommand command)
    {
        var result = new CliCommandResult
        {
            Command = command.Command,
            Success = false,
            Output = "导入功能需要更多参数",
            Errors = new List<string>()
        };

        // 导入逻辑
        result.Success = true;
        return result;
    }

    private string GetHelpText()
    {
        return @"SafeTool CLI 命令帮助

可用命令:
  evaluate          - 执行单个评估
  batch-evaluate    - 批量执行评估
  generate-report   - 生成报告
  batch-report      - 批量生成报告
  export            - 导出数据
  import            - 导入数据

使用示例:
  safetool evaluate --standard ISO13849-1 --checklist checklist.json
  safetool batch-evaluate --requests requests.json
  safetool batch-report --requests reports.json --format pdf --language en-US

更多信息请参考文档。";
    }
}

public class CliCommand
{
    public string Command { get; set; } = string.Empty;
    public Dictionary<string, object>? Arguments { get; set; }
    public Dictionary<string, string>? Options { get; set; }
}

public class CliCommandResult
{
    public string Command { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}

