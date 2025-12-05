using SafeTool.Domain.Standards;

namespace SafeTool.Application.Services;

/// <summary>
/// 类别自动推导增强服务
/// </summary>
public class CategoryDerivationEnhancementService
{
    private readonly Iso13849CalculationEnhancementService _calculationService;

    public CategoryDerivationEnhancementService(Iso13849CalculationEnhancementService calculationService)
    {
        _calculationService = calculationService;
    }

    /// <summary>
    /// 增强的类别自动推导
    /// </summary>
    public EnhancedCategoryDerivationResult DeriveCategory(EnhancedCategoryDerivationInput input)
    {
        var result = new EnhancedCategoryDerivationResult
        {
            Suggestions = new List<CategorySuggestion>(),
            Conflicts = new List<CategoryConflict>(),
            Recommendations = new List<string>(),
            AnalysisSteps = new List<DerivationStep>()
        };

        // 步骤1：分析架构特征
        AnalyzeArchitecture(input, result);

        // 步骤2：分析监测特征
        AnalyzeMonitoring(input, result);

        // 步骤3：分析冗余特征
        AnalyzeRedundancy(input, result);

        // 步骤4：分析CCF评分
        AnalyzeCCF(input, result);

        // 步骤5：生成类别建议
        GenerateCategorySuggestions(input, result);

        // 步骤6：检查冲突
        CheckConflicts(input, result);

        // 步骤7：生成整改建议
        GenerateRecommendations(input, result);

        return result;
    }

    private void AnalyzeArchitecture(EnhancedCategoryDerivationInput input, EnhancedCategoryDerivationResult result)
    {
        result.AnalysisSteps.Add(new DerivationStep
        {
            Step = 1,
            Description = "分析架构特征",
            Details = new List<string>()
        });

        var hasInput = input.InputChannelCount > 0;
        var hasLogic = input.LogicChannelCount > 0;
        var hasOutput = input.OutputChannelCount > 0;

        result.AnalysisSteps.Last().Details.Add($"输入通道数: {input.InputChannelCount}");
        result.AnalysisSteps.Last().Details.Add($"逻辑通道数: {input.LogicChannelCount}");
        result.AnalysisSteps.Last().Details.Add($"输出通道数: {input.OutputChannelCount}");

        if (!hasInput && !hasLogic && !hasOutput)
        {
            result.Conflicts.Add(new CategoryConflict
            {
                Type = "MissingChannels",
                Severity = "High",
                Message = "缺少通道定义，无法推导类别"
            });
        }
    }

    private void AnalyzeMonitoring(EnhancedCategoryDerivationInput input, EnhancedCategoryDerivationResult result)
    {
        result.AnalysisSteps.Add(new DerivationStep
        {
            Step = 2,
            Description = "分析监测特征",
            Details = new List<string>()
        });

        var hasMonitoring = input.HasInputMonitoring || input.HasLogicMonitoring || input.HasOutputMonitoring;
        var hasTestEquipment = input.HasTestEquipment;

        result.AnalysisSteps.Last().Details.Add($"输入监测: {input.HasInputMonitoring}");
        result.AnalysisSteps.Last().Details.Add($"逻辑监测: {input.HasLogicMonitoring}");
        result.AnalysisSteps.Last().Details.Add($"输出监测: {input.HasOutputMonitoring}");
        result.AnalysisSteps.Last().Details.Add($"测试设备: {hasTestEquipment}");

        if (hasMonitoring || hasTestEquipment)
        {
            result.AnalysisSteps.Last().Details.Add("✓ 检测到监测或测试功能");
        }
        else
        {
            result.AnalysisSteps.Last().Details.Add("⚠️ 未检测到监测或测试功能");
        }
    }

    private void AnalyzeRedundancy(EnhancedCategoryDerivationInput input, EnhancedCategoryDerivationResult result)
    {
        result.AnalysisSteps.Add(new DerivationStep
        {
            Step = 3,
            Description = "分析冗余特征",
            Details = new List<string>()
        });

        var hasRedundancy = input.InputChannelCount >= 2 || 
                           input.LogicChannelCount >= 2 || 
                           input.OutputChannelCount >= 2;

        result.AnalysisSteps.Last().Details.Add($"输入冗余: {input.InputChannelCount >= 2}");
        result.AnalysisSteps.Last().Details.Add($"逻辑冗余: {input.LogicChannelCount >= 2}");
        result.AnalysisSteps.Last().Details.Add($"输出冗余: {input.OutputChannelCount >= 2}");
        result.AnalysisSteps.Last().Details.Add($"总体冗余: {hasRedundancy}");

        if (hasRedundancy)
        {
            result.AnalysisSteps.Last().Details.Add("✓ 检测到冗余架构");
        }
    }

    private void AnalyzeCCF(EnhancedCategoryDerivationInput input, EnhancedCategoryDerivationResult result)
    {
        result.AnalysisSteps.Add(new DerivationStep
        {
            Step = 4,
            Description = "分析CCF评分",
            Details = new List<string>()
        });

        if (input.CCFScore.HasValue)
        {
            result.AnalysisSteps.Last().Details.Add($"CCF评分: {input.CCFScore.Value}");
            
            if (input.CCFScore.Value >= 65)
            {
                result.AnalysisSteps.Last().Details.Add("✓ CCF评分满足要求（≥65）");
            }
            else
            {
                result.AnalysisSteps.Last().Details.Add($"⚠️ CCF评分不足（{input.CCFScore.Value} < 65）");
            }
        }
        else
        {
            result.AnalysisSteps.Last().Details.Add("⚠️ 缺少CCF评分信息");
        }
    }

    private void GenerateCategorySuggestions(EnhancedCategoryDerivationInput input, EnhancedCategoryDerivationResult result)
    {
        result.AnalysisSteps.Add(new DerivationStep
        {
            Step = 5,
            Description = "生成类别建议",
            Details = new List<string>()
        });

        var hasRedundancy = input.InputChannelCount >= 2 || 
                           input.LogicChannelCount >= 2 || 
                           input.OutputChannelCount >= 2;
        var hasMonitoring = input.HasInputMonitoring || input.HasLogicMonitoring || input.HasOutputMonitoring;
        var hasTestEquipment = input.HasTestEquipment;
        var ccfOk = input.CCFScore.HasValue && input.CCFScore.Value >= 65;

        // Category B
        result.Suggestions.Add(new CategorySuggestion
        {
            Category = "B",
            Confidence = 0.9,
            Reason = "基础类别，适用于所有安全功能",
            Requirements = new List<string> { "无特殊要求" }
        });

        // Category 1
        if (!hasRedundancy && !hasMonitoring && !hasTestEquipment)
        {
            result.Suggestions.Add(new CategorySuggestion
            {
                Category = "1",
                Confidence = 0.8,
                Reason = "单通道架构，无监测",
                Requirements = new List<string> { "单通道", "无监测要求" }
            });
        }

        // Category 2
        if (hasTestEquipment && !hasRedundancy)
        {
            result.Suggestions.Add(new CategorySuggestion
            {
                Category = "2",
                Confidence = 0.85,
                Reason = "单通道架构，具有测试设备",
                Requirements = new List<string> { "单通道", "测试设备" }
            });
        }

        // Category 3
        if (hasRedundancy && ccfOk)
        {
            result.Suggestions.Add(new CategorySuggestion
            {
                Category = "3",
                Confidence = 0.9,
                Reason = "冗余架构，CCF评分满足要求",
                Requirements = new List<string> { "冗余架构", "CCF≥65" }
            });
        }

        // Category 4
        if (hasRedundancy && hasMonitoring && ccfOk)
        {
            result.Suggestions.Add(new CategorySuggestion
            {
                Category = "4",
                Confidence = 0.95,
                Reason = "冗余架构，具有监测功能，CCF评分满足要求",
                Requirements = new List<string> { "冗余架构", "监测功能", "CCF≥65" }
            });
        }

        // 按置信度排序
        result.Suggestions = result.Suggestions.OrderByDescending(s => s.Confidence).ToList();
    }

    private void CheckConflicts(EnhancedCategoryDerivationInput input, EnhancedCategoryDerivationResult result)
    {
        result.AnalysisSteps.Add(new DerivationStep
        {
            Step = 6,
            Description = "检查冲突",
            Details = new List<string>()
        });

        var hasRedundancy = input.InputChannelCount >= 2 || 
                           input.LogicChannelCount >= 2 || 
                           input.OutputChannelCount >= 2;
        var ccfOk = input.CCFScore.HasValue && input.CCFScore.Value >= 65;

        // 检查Category 3/4但无冗余
        var suggestedCat3Or4 = result.Suggestions.Any(s => s.Category == "3" || s.Category == "4");
        if (suggestedCat3Or4 && !hasRedundancy)
        {
            result.Conflicts.Add(new CategoryConflict
            {
                Type = "CategoryWithoutRedundancy",
                Severity = "High",
                Message = "建议类别为Cat3或Cat4，但未检测到冗余架构"
            });
        }

        // 检查Category 3/4但CCF不足
        if (suggestedCat3Or4 && !ccfOk)
        {
            result.Conflicts.Add(new CategoryConflict
            {
                Type = "CategoryWithLowCCF",
                Severity = "High",
                Message = "建议类别为Cat3或Cat4，但CCF评分不足（<65）"
            });
        }

        // 检查Category 2但无测试设备
        var suggestedCat2 = result.Suggestions.Any(s => s.Category == "2");
        if (suggestedCat2 && !input.HasTestEquipment)
        {
            result.Conflicts.Add(new CategoryConflict
            {
                Type = "Category2WithoutTestEquipment",
                Severity = "Medium",
                Message = "建议类别为Cat2，但未配置测试设备"
            });
        }
    }

    private void GenerateRecommendations(EnhancedCategoryDerivationInput input, EnhancedCategoryDerivationResult result)
    {
        result.AnalysisSteps.Add(new DerivationStep
        {
            Step = 7,
            Description = "生成整改建议",
            Details = new List<string>()
        });

        // 基于冲突生成建议
        foreach (var conflict in result.Conflicts)
        {
            switch (conflict.Type)
            {
                case "CategoryWithoutRedundancy":
                    result.Recommendations.Add("增加冗余通道以实现Cat3/Cat4架构");
                    break;
                case "CategoryWithLowCCF":
                    result.Recommendations.Add("提高CCF评分至65分以上（参考Annex F）");
                    break;
                case "Category2WithoutTestEquipment":
                    result.Recommendations.Add("配置测试设备以支持Cat2架构");
                    break;
            }
        }

        // 基于建议生成优化建议
        var topSuggestion = result.Suggestions.FirstOrDefault();
        if (topSuggestion != null)
        {
            result.Recommendations.Add($"推荐类别: {topSuggestion.Category}（置信度: {topSuggestion.Confidence:P0}）");
            result.Recommendations.Add($"理由: {topSuggestion.Reason}");
        }
    }
}

public class EnhancedCategoryDerivationInput
{
    public int InputChannelCount { get; set; }
    public int LogicChannelCount { get; set; }
    public int OutputChannelCount { get; set; }
    public bool HasInputMonitoring { get; set; }
    public bool HasLogicMonitoring { get; set; }
    public bool HasOutputMonitoring { get; set; }
    public bool HasTestEquipment { get; set; }
    public double? CCFScore { get; set; }
}

public class EnhancedCategoryDerivationResult
{
    public List<CategorySuggestion> Suggestions { get; set; } = new();
    public List<CategoryConflict> Conflicts { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<DerivationStep> AnalysisSteps { get; set; } = new();
}

// CategorySuggestion定义（与Iso13849CalculationEnhancementService共享）
public class CategorySuggestion
{
    public string Category { get; set; } = string.Empty; // B/1/2/3/4
    public double Confidence { get; set; } // 0-1
    public string Reason { get; set; } = string.Empty;
    public List<string> Requirements { get; set; } = new();
}

public class CategoryConflict
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Low/Medium/High
    public string Message { get; set; } = string.Empty;
}

public class DerivationStep
{
    public int Step { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Details { get; set; } = new();
}

