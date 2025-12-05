using SafeTool.Application.Services;

namespace SafeTool.Application.Services;

/// <summary>
/// CCF评分向导服务（建造者模式 + 策略模式）
/// </summary>
public class CcfWizardService
{
    private readonly CcfService _ccfService;
    private readonly EvidenceService _evidenceService;

    public CcfWizardService(CcfService ccfService, EvidenceService evidenceService)
    {
        _ccfService = ccfService;
        _evidenceService = evidenceService;
    }

    /// <summary>
    /// 创建CCF评分向导（建造者模式）
    /// </summary>
    public CcfWizard CreateWizard()
    {
        return new CcfWizard(_ccfService, _evidenceService);
    }

    /// <summary>
    /// 获取评分建议（策略模式）
    /// </summary>
    public CcfRecommendation GetRecommendation(int currentScore, IEnumerable<string> selectedCodes)
    {
        var allItems = _ccfService.GetItems();
        var selectedSet = new HashSet<string>(selectedCodes);
        var availableItems = allItems.Where(i => !selectedSet.Contains(i.Code)).ToList();
        
        var recommendation = new CcfRecommendation
        {
            CurrentScore = currentScore,
            TargetScore = 65,
            Gap = Math.Max(0, 65 - currentScore),
            Suggestions = new List<CcfSuggestion>()
        };

        if (currentScore < 65)
        {
            // 按分数排序，优先推荐高分项
            var sortedItems = availableItems.OrderByDescending(i => i.Score).ToList();
            
            foreach (var item in sortedItems)
            {
                if (recommendation.Gap <= 0)
                    break;
                
                recommendation.Suggestions.Add(new CcfSuggestion
                {
                    Code = item.Code,
                    Title = item.Title,
                    Score = item.Score,
                    Reason = GetSuggestionReason(item.Code),
                    Priority = GetPriority(item.Score, recommendation.Gap)
                });
                
                recommendation.Gap -= item.Score;
            }
        }
        else
        {
            recommendation.Message = "当前评分已达到65分阈值，符合要求";
        }

        return recommendation;
    }

    private string GetSuggestionReason(string code)
    {
        return code switch
        {
            "CCF-ENV" => "环境分离与防护是基础要求，建议优先实施",
            "CCF-RED" => "冗余多样化可显著提高系统可靠性，建议实施",
            "CCF-EMC" => "EMC设计与验证对电子系统至关重要",
            "CCF-WIR" => "布线与隔离可减少共因失效风险",
            "CCF-MNT" => "维护与周期测试确保系统长期可靠性",
            "CCF-DIV" => "逻辑与通道多样化提高系统鲁棒性",
            "CCF-QA" => "质量流程与变更控制是系统化管理的基础",
            "CCF-DOC" => "文档与培训确保操作和维护的正确性",
            _ => "建议实施此措施以提高CCF评分"
        };
    }

    private CcfSuggestionPriority GetPriority(int score, int gap)
    {
        if (score >= gap)
            return CcfSuggestionPriority.High;
        if (score >= gap / 2)
            return CcfSuggestionPriority.Medium;
        return CcfSuggestionPriority.Low;
    }
}

/// <summary>
/// CCF评分向导（建造者模式）
/// </summary>
public class CcfWizard
{
    private readonly CcfService _ccfService;
    private readonly EvidenceService _evidenceService;
    private readonly List<CcfWizardStep> _steps = new();
    private int _currentStepIndex = 0;
    private readonly HashSet<string> _selectedCodes = new();

    public CcfWizard(CcfService ccfService, EvidenceService evidenceService)
    {
        _ccfService = ccfService;
        _evidenceService = evidenceService;
        InitializeSteps();
    }

    private void InitializeSteps()
    {
        var items = _ccfService.GetItems().ToList();
        
        // 按重要性分组步骤
        _steps.Add(new CcfWizardStep
        {
            StepNumber = 1,
            Title = "环境分离与防护",
            Description = "评估环境分离与防护措施（温湿度/粉尘/液体防护）",
            Items = items.Where(i => i.Code == "CCF-ENV").ToList(),
            IsRequired = true
        });
        
        _steps.Add(new CcfWizardStep
        {
            StepNumber = 2,
            Title = "冗余多样化",
            Description = "评估冗余多样化措施（不同原理/不同供应商）",
            Items = items.Where(i => i.Code == "CCF-RED").ToList(),
            IsRequired = true
        });
        
        _steps.Add(new CcfWizardStep
        {
            StepNumber = 3,
            Title = "EMC设计与验证",
            Description = "评估EMC设计与验证措施（接地/滤波/试验）",
            Items = items.Where(i => i.Code == "CCF-EMC").ToList(),
            IsRequired = true
        });
        
        _steps.Add(new CcfWizardStep
        {
            StepNumber = 4,
            Title = "其他措施",
            Description = "评估其他CCF措施（布线/维护/多样化/质量/文档）",
            Items = items.Where(i => !new[] { "CCF-ENV", "CCF-RED", "CCF-EMC" }.Contains(i.Code)).ToList(),
            IsRequired = false
        });
    }

    public CcfWizardStep? GetCurrentStep()
    {
        if (_currentStepIndex >= _steps.Count)
            return null;
        return _steps[_currentStepIndex];
    }

    public CcfWizardStep? GetNextStep()
    {
        if (_currentStepIndex < _steps.Count - 1)
        {
            _currentStepIndex++;
            return GetCurrentStep();
        }
        return null;
    }

    public CcfWizardStep? GetPreviousStep()
    {
        if (_currentStepIndex > 0)
        {
            _currentStepIndex--;
            return GetCurrentStep();
        }
        return null;
    }

    public void SelectItem(string code)
    {
        _selectedCodes.Add(code);
    }

    public void DeselectItem(string code)
    {
        _selectedCodes.Remove(code);
    }

    public CcfWizardResult GetResult()
    {
        var score = _ccfService.ComputeScore(_selectedCodes);
        var isPassed = score >= 65;
        
        // 自动关联证据
        var evidenceSuggestions = new List<string>();
        foreach (var code in _selectedCodes)
        {
            evidenceSuggestions.Add($"建议为 {code} 措施提供证据（证书/测试报告/照片等）");
        }
        
        return new CcfWizardResult
        {
            SelectedCodes = _selectedCodes.ToList(),
            Score = score,
            IsPassed = isPassed,
            Message = isPassed 
                ? $"恭喜！CCF评分 {score} 分，已达到65分阈值要求" 
                : $"当前CCF评分 {score} 分，未达到65分阈值要求，建议增加措施",
            EvidenceSuggestions = evidenceSuggestions,
            CompletedAt = DateTime.UtcNow
        };
    }

    public bool IsComplete()
    {
        return _currentStepIndex >= _steps.Count;
    }
}

public class CcfWizardStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<CcfItem> Items { get; set; } = new();
    public bool IsRequired { get; set; }
}

public class CcfWizardResult
{
    public List<string> SelectedCodes { get; set; } = new();
    public int Score { get; set; }
    public bool IsPassed { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> EvidenceSuggestions { get; set; } = new();
    public DateTime CompletedAt { get; set; }
}

public class CcfRecommendation
{
    public int CurrentScore { get; set; }
    public int TargetScore { get; set; }
    public int Gap { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<CcfSuggestion> Suggestions { get; set; } = new();
}

public class CcfSuggestion
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public CcfSuggestionPriority Priority { get; set; }
}

public enum CcfSuggestionPriority
{
    Low,
    Medium,
    High
}

