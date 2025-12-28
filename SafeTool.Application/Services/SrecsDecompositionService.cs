namespace SafeTool.Application.Services;

/// <summary>
/// SRECS结构化分解提示服务（P2优先级）
/// 为IEC 62061提供SRECS结构化分解建议
/// </summary>
public class SrecsDecompositionService
{
    /// <summary>
    /// 分析SRECS结构并生成分解建议
    /// </summary>
    public SrecsDecompositionResult AnalyzeAndDecompose(
        SafeTool.Domain.Standards.SafetyFunction62061 function)
    {
        var result = new SrecsDecompositionResult
        {
            FunctionId = function.Id,
            FunctionName = function.Name,
            AnalyzedAt = DateTime.UtcNow
        };

        // 1. 分析当前结构
        result.StructureAnalysis = AnalyzeStructure(function);

        // 2. 生成分解建议
        result.DecompositionSuggestions = GenerateDecompositionSuggestions(function, result.StructureAnalysis);

        // 3. 生成简化提示
        result.SimplificationHints = GenerateSimplificationHints(function, result.StructureAnalysis);

        // 4. 生成方程提示
        result.EquationHints = GenerateEquationHints(function, result.StructureAnalysis);

        return result;
    }

    /// <summary>
    /// 分析结构
    /// </summary>
    private SrecsStructureAnalysis AnalyzeStructure(SafeTool.Domain.Standards.SafetyFunction62061 function)
    {
        var analysis = new SrecsStructureAnalysis
        {
            SubsystemCount = function.Subsystems.Count,
            TotalComponents = function.Subsystems.Sum(s => s.Components.Count),
            ArchitectureTypes = function.Subsystems.Select(s => s.Architecture).Distinct().ToList(),
            ComplexityLevel = DetermineComplexity(function)
        };

        // 分析子系统层级
        foreach (var subsystem in function.Subsystems)
        {
            analysis.SubsystemDetails.Add(new SubsystemDetail
            {
                Id = subsystem.Id,
                Name = subsystem.Name,
                Architecture = subsystem.Architecture,
                ComponentCount = subsystem.Components.Count,
                PFHd = subsystem.PFHdCalculated,
                Complexity = CalculateSubsystemComplexity(subsystem)
            });
        }

        return analysis;
    }

    /// <summary>
    /// 生成分解建议
    /// </summary>
    private List<DecompositionSuggestion> GenerateDecompositionSuggestions(
        SafeTool.Domain.Standards.SafetyFunction62061 function,
        SrecsStructureAnalysis analysis)
    {
        var suggestions = new List<DecompositionSuggestion>();

        // 1. 复杂子系统分解建议
        if (analysis.ComplexityLevel == "High" || analysis.ComplexityLevel == "VeryHigh")
        {
            suggestions.Add(new DecompositionSuggestion
            {
                Type = "复杂子系统分解",
                Description = "检测到复杂子系统结构，建议分解为更简单的子系统",
                Reason = $"当前复杂度: {analysis.ComplexityLevel}，子系统数: {analysis.SubsystemCount}",
                Benefit = "简化计算，提高可维护性",
                Steps = new List<string>
                {
                    "识别功能独立的子系统",
                    "将复杂子系统分解为多个简单子系统",
                    "重新计算各子系统的PFHd",
                    "验证分解后的总PFHd是否满足要求"
                }
            });
        }

        // 2. 混合架构分解建议
        var hasMixedArchitecture = analysis.ArchitectureTypes.Count > 1;
        if (hasMixedArchitecture)
        {
            suggestions.Add(new DecompositionSuggestion
            {
                Type = "混合架构分解",
                Description = "检测到混合架构（1oo1/1oo2/2oo3），建议按架构类型分组",
                Reason = $"包含 {analysis.ArchitectureTypes.Count} 种不同的架构类型",
                Benefit = "便于理解和维护，简化方程",
                Steps = new List<string>
                {
                    "按架构类型分组子系统",
                    "分别计算各组的总PFHd",
                    "合并各组结果得到系统总PFHd"
                }
            });
        }

        // 3. 大型子系统分解建议
        var largeSubsystems = analysis.SubsystemDetails
            .Where(s => s.ComponentCount > 5)
            .ToList();

        if (largeSubsystems.Any())
        {
            suggestions.Add(new DecompositionSuggestion
            {
                Type = "大型子系统分解",
                Description = $"检测到 {largeSubsystems.Count} 个大型子系统（组件数>5），建议进一步分解",
                Reason = "大型子系统难以理解和维护",
                Benefit = "提高可读性和可维护性",
                Steps = new List<string>
                {
                    "识别子系统中的功能模块",
                    "将大型子系统分解为多个功能模块",
                    "为每个功能模块计算PFHd",
                    "合并功能模块得到子系统PFHd"
                }
            });
        }

        return suggestions;
    }

    /// <summary>
    /// 生成简化提示
    /// </summary>
    private List<SimplificationHint> GenerateSimplificationHints(
        SafeTool.Domain.Standards.SafetyFunction62061 function,
        SrecsStructureAnalysis analysis)
    {
        var hints = new List<SimplificationHint>();

        // 1. 串联1oo1简化
        var series1oo1 = function.Subsystems
            .Where(s => s.Architecture == "1oo1")
            .ToList();

        if (series1oo1.Count > 1)
        {
            hints.Add(new SimplificationHint
            {
                Type = "串联1oo1简化",
                Description = $"发现 {series1oo1.Count} 个串联的1oo1子系统，可以合并计算",
                OriginalEquation = $"PFHd_total = Σ(PFHd_i) for i=1 to {series1oo1.Count}",
                SimplifiedEquation = $"PFHd_total = Σ(PFHd_i) (直接求和)",
                Benefit = "简化计算，无需复杂公式"
            });
        }

        // 2. 相同PFHd组件简化
        var componentGroups = function.Subsystems
            .SelectMany(s => s.Components)
            .GroupBy(c => c.PFHd)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in componentGroups)
        {
            hints.Add(new SimplificationHint
            {
                Type = "相同PFHd组件简化",
                Description = $"发现 {group.Count()} 个组件具有相同的PFHd ({group.Key:E2})",
                OriginalEquation = $"PFHd = {group.Count()} × PFHd_component",
                SimplifiedEquation = $"PFHd = {group.Count()} × {group.Key:E2}",
                Benefit = "减少计算步骤"
            });
        }

        return hints;
    }

    /// <summary>
    /// 生成方程提示
    /// </summary>
    private List<EquationHint> GenerateEquationHints(
        SafeTool.Domain.Standards.SafetyFunction62061 function,
        SrecsStructureAnalysis analysis)
    {
        var hints = new List<EquationHint>();

        // 为每种架构类型生成方程提示
        foreach (var archType in analysis.ArchitectureTypes)
        {
            var subsystems = function.Subsystems.Where(s => s.Architecture == archType).ToList();
            if (subsystems.Any())
            {
                hints.Add(new EquationHint
                {
                    Architecture = archType,
                    SubsystemCount = subsystems.Count,
                    Equation = GetEquationForArchitecture(archType),
                    Explanation = GetEquationExplanation(archType),
                    Example = GetEquationExample(archType, subsystems)
                });
            }
        }

        return hints;
    }

    /// <summary>
    /// 确定复杂度
    /// </summary>
    private string DetermineComplexity(SafeTool.Domain.Standards.SafetyFunction62061 function)
    {
        var subsystemCount = function.Subsystems.Count;
        var totalComponents = function.Subsystems.Sum(s => s.Components.Count);
        var architectureTypes = function.Subsystems.Select(s => s.Architecture).Distinct().Count();

        if (subsystemCount > 10 || totalComponents > 30 || architectureTypes > 3)
            return "VeryHigh";
        else if (subsystemCount > 5 || totalComponents > 15 || architectureTypes > 2)
            return "High";
        else if (subsystemCount > 3 || totalComponents > 10)
            return "Medium";
        else
            return "Low";
    }

    /// <summary>
    /// 计算子系统复杂度
    /// </summary>
    private string CalculateSubsystemComplexity(SafeTool.Domain.Standards.IEC62061Subsystem subsystem)
    {
        if (subsystem.Components.Count > 10)
            return "VeryHigh";
        else if (subsystem.Components.Count > 5)
            return "High";
        else if (subsystem.Components.Count > 3)
            return "Medium";
        else
            return "Low";
    }

    /// <summary>
    /// 获取架构方程
    /// </summary>
    private string GetEquationForArchitecture(string architecture)
    {
        return architecture switch
        {
            "1oo1" => "PFHd = Σ(PFHd_i)",
            "1oo2" => "PFHd = 2 × (1 - β) × λ_D² × t_CE + β × λ_D",
            "2oo3" => "PFHd = 6 × (1 - β) × λ_D² × t_CE + β × λ_D",
            _ => "PFHd = f(architecture, components)"
        };
    }

    /// <summary>
    /// 获取方程说明
    /// </summary>
    private string GetEquationExplanation(string architecture)
    {
        return architecture switch
        {
            "1oo1" => "单通道架构：PFHd等于所有组件PFHd之和",
            "1oo2" => "1oo2架构：考虑共因失效和诊断覆盖率",
            "2oo3" => "2oo3架构：三取二表决，考虑共因失效",
            _ => "其他架构：需要根据具体架构计算"
        };
    }

    /// <summary>
    /// 获取方程示例
    /// </summary>
    private string GetEquationExample(string architecture, List<SafeTool.Domain.Standards.IEC62061Subsystem> subsystems)
    {
        if (subsystems.Any())
        {
            var firstSubsystem = subsystems.First();
            return $"示例：{firstSubsystem.Name}，PFHd = {firstSubsystem.PFHdCalculated:E2}";
        }
        return "无示例";
    }
}

public class SrecsDecompositionResult
{
    public string FunctionId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
    public SrecsStructureAnalysis? StructureAnalysis { get; set; }
    public List<DecompositionSuggestion> DecompositionSuggestions { get; set; } = new();
    public List<SimplificationHint> SimplificationHints { get; set; } = new();
    public List<EquationHint> EquationHints { get; set; } = new();
}

public class SrecsStructureAnalysis
{
    public int SubsystemCount { get; set; }
    public int TotalComponents { get; set; }
    public List<string> ArchitectureTypes { get; set; } = new();
    public string ComplexityLevel { get; set; } = "Low";
    public List<SubsystemDetail> SubsystemDetails { get; set; } = new();
}

public class SubsystemDetail
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public int ComponentCount { get; set; }
    public double PFHd { get; set; }
    public string Complexity { get; set; } = "Low";
}

public class DecompositionSuggestion
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Benefit { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = new();
}

public class SimplificationHint
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OriginalEquation { get; set; } = string.Empty;
    public string SimplifiedEquation { get; set; } = string.Empty;
    public string Benefit { get; set; } = string.Empty;
}

public class EquationHint
{
    public string Architecture { get; set; } = string.Empty;
    public int SubsystemCount { get; set; }
    public string Equation { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
}

