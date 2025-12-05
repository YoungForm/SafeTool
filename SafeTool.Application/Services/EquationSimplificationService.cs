namespace SafeTool.Application.Services;

/// <summary>
/// 方程简化提示服务（P2优先级）
/// 为IEC 62061提供方程简化建议
/// </summary>
public class EquationSimplificationService
{
    /// <summary>
    /// 分析方程并生成简化建议
    /// </summary>
    public EquationSimplificationResult AnalyzeAndSimplify(
        SafeTool.Domain.Standards.SafetyFunction62061 function)
    {
        var result = new EquationSimplificationResult
        {
            FunctionId = function.Id,
            FunctionName = function.Name,
            AnalyzedAt = DateTime.UtcNow
        };

        // 1. 分析子系统结构
        result.StructureAnalysis = AnalyzeStructure(function);

        // 2. 识别可简化项
        result.SimplificationOpportunities = IdentifySimplificationOpportunities(function);

        // 3. 生成简化建议
        result.Suggestions = GenerateSimplificationSuggestions(result);

        // 4. 计算简化后的PFHd（如果可能）
        if (result.Suggestions.Any(s => s.CanSimplify))
        {
            result.SimplifiedPFHd = CalculateSimplifiedPFHd(function, result);
        }

        return result;
    }

    /// <summary>
    /// 分析结构
    /// </summary>
    private StructureAnalysis AnalyzeStructure(SafeTool.Domain.Standards.SafetyFunction62061 function)
    {
        var analysis = new StructureAnalysis
        {
            SubsystemCount = function.Subsystems.Count,
            TotalComponents = function.Subsystems.Sum(s => s.Components.Count),
            RedundantStructures = new List<string>(),
            SeriesStructures = new List<string>()
        };

        foreach (var subsystem in function.Subsystems)
        {
            if (subsystem.Architecture.Contains("oo2") || subsystem.Architecture.Contains("oo3"))
            {
                analysis.RedundantStructures.Add($"{subsystem.Id}: {subsystem.Architecture}");
            }
            else
            {
                analysis.SeriesStructures.Add($"{subsystem.Id}: {subsystem.Architecture}");
            }
        }

        return analysis;
    }

    /// <summary>
    /// 识别简化机会
    /// </summary>
    private List<SimplificationOpportunity> IdentifySimplificationOpportunities(
        SafeTool.Domain.Standards.SafetyFunction62061 function)
    {
        var opportunities = new List<SimplificationOpportunity>();

        // 1. 检查是否有相同PFHd的组件可以合并
        var componentGroups = function.Subsystems
            .SelectMany(s => s.Components)
            .GroupBy(c => c.PFHd)
            .Where(g => g.Count() > 1);

        foreach (var group in componentGroups)
        {
            opportunities.Add(new SimplificationOpportunity
            {
                Type = "相同PFHd组件合并",
                Description = $"发现 {group.Count()} 个组件具有相同的PFHd ({group.Key:E2})，可以合并计算",
                Components = group.Select(c => c.Id).ToList(),
                Benefit = "减少计算复杂度",
                CanSimplify = true
            });
        }

        // 2. 检查是否有串联的1oo1结构可以简化
        var series1oo1 = function.Subsystems
            .Where(s => s.Architecture == "1oo1" && s.Components.Count == 1)
            .ToList();

        if (series1oo1.Count > 1)
        {
            opportunities.Add(new SimplificationOpportunity
            {
                Type = "串联1oo1简化",
                Description = $"发现 {series1oo1.Count} 个串联的1oo1子系统，可以合并为一个等效子系统",
                Subsystems = series1oo1.Select(s => s.Id).ToList(),
                Benefit = "简化方程结构",
                CanSimplify = true
            });
        }

        // 3. 检查是否有低PFHd组件可以忽略
        var lowPFHdComponents = function.Subsystems
            .SelectMany(s => s.Components)
            .Where(c => c.PFHd > 0 && c.PFHd < 1e-10)
            .ToList();

        if (lowPFHdComponents.Any())
        {
            opportunities.Add(new SimplificationOpportunity
            {
                Type = "低PFHd组件忽略",
                Description = $"发现 {lowPFHdComponents.Count} 个PFHd极低的组件（<1e-10），对总PFHd贡献可忽略",
                Components = lowPFHdComponents.Select(c => c.Id).ToList(),
                Benefit = "简化计算，误差可忽略",
                CanSimplify = true
            });
        }

        return opportunities;
    }

    /// <summary>
    /// 生成简化建议
    /// </summary>
    private List<SimplificationSuggestion> GenerateSimplificationSuggestions(
        EquationSimplificationResult result)
    {
        var suggestions = new List<SimplificationSuggestion>();

        foreach (var opportunity in result.SimplificationOpportunities)
        {
            if (opportunity.CanSimplify)
            {
                suggestions.Add(new SimplificationSuggestion
                {
                    Type = opportunity.Type,
                    Description = opportunity.Description,
                    OriginalEquation = GenerateOriginalEquation(opportunity),
                    SimplifiedEquation = GenerateSimplifiedEquation(opportunity),
                    Benefit = opportunity.Benefit,
                    CanSimplify = true,
                    Steps = GenerateSimplificationSteps(opportunity)
                });
            }
        }

        return suggestions;
    }

    /// <summary>
    /// 生成原始方程
    /// </summary>
    private string GenerateOriginalEquation(SimplificationOpportunity opportunity)
    {
        if (opportunity.Type == "相同PFHd组件合并")
        {
            var count = opportunity.Components?.Count ?? 0;
            return $"PFHd_total = {count} × PFHd_component";
        }
        else if (opportunity.Type == "串联1oo1简化")
        {
            var count = opportunity.Subsystems?.Count ?? 0;
            return $"PFHd_total = Σ(PFHd_i) for i=1 to {count}";
        }

        return "原始方程";
    }

    /// <summary>
    /// 生成简化方程
    /// </summary>
    private string GenerateSimplifiedEquation(SimplificationOpportunity opportunity)
    {
        if (opportunity.Type == "相同PFHd组件合并")
        {
            var count = opportunity.Components?.Count ?? 0;
            return $"PFHd_total = {count} × PFHd_component (合并计算)";
        }
        else if (opportunity.Type == "串联1oo1简化")
        {
            return "PFHd_total = PFHd_equivalent (等效子系统)";
        }

        return "简化方程";
    }

    /// <summary>
    /// 生成简化步骤
    /// </summary>
    private List<string> GenerateSimplificationSteps(SimplificationOpportunity opportunity)
    {
        var steps = new List<string>();

        if (opportunity.Type == "相同PFHd组件合并")
        {
            steps.Add("识别具有相同PFHd的组件");
            steps.Add("将这些组件合并为一个等效组件");
            steps.Add("使用合并后的PFHd进行计算");
        }
        else if (opportunity.Type == "串联1oo1简化")
        {
            steps.Add("识别串联的1oo1子系统");
            steps.Add("计算等效PFHd = Σ(PFHd_i)");
            steps.Add("使用等效子系统替换原始子系统");
        }

        return steps;
    }

    /// <summary>
    /// 计算简化后的PFHd
    /// </summary>
    private double? CalculateSimplifiedPFHd(
        SafeTool.Domain.Standards.SafetyFunction62061 function,
        EquationSimplificationResult result)
    {
        // 这里可以实现实际的简化计算逻辑
        // 目前返回null，表示需要手动计算
        return null;
    }
}

public class EquationSimplificationResult
{
    public string FunctionId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
    public StructureAnalysis? StructureAnalysis { get; set; }
    public List<SimplificationOpportunity> SimplificationOpportunities { get; set; } = new();
    public List<SimplificationSuggestion> Suggestions { get; set; } = new();
    public double? SimplifiedPFHd { get; set; }
}

public class StructureAnalysis
{
    public int SubsystemCount { get; set; }
    public int TotalComponents { get; set; }
    public List<string> RedundantStructures { get; set; } = new();
    public List<string> SeriesStructures { get; set; } = new();
}

public class SimplificationOpportunity
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string>? Components { get; set; }
    public List<string>? Subsystems { get; set; }
    public string Benefit { get; set; } = string.Empty;
    public bool CanSimplify { get; set; }
}

public class SimplificationSuggestion
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OriginalEquation { get; set; } = string.Empty;
    public string SimplifiedEquation { get; set; } = string.Empty;
    public string Benefit { get; set; } = string.Empty;
    public bool CanSimplify { get; set; }
    public List<string> Steps { get; set; } = new();
}

