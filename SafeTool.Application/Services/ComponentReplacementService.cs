using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 组件替代建议服务（P2优先级）
/// 根据组件参数推荐替代组件
/// </summary>
public class ComponentReplacementService
{
    private readonly ComponentLibraryService _componentLibrary;

    public ComponentReplacementService(ComponentLibraryService componentLibrary)
    {
        _componentLibrary = componentLibrary;
    }

    /// <summary>
    /// 获取组件替代建议
    /// </summary>
    public ComponentReplacementResult GetReplacementSuggestions(string componentId, ReplacementCriteria? criteria = null)
    {
        var component = _componentLibrary.Get(componentId);
        if (component == null)
        {
            return new ComponentReplacementResult
            {
                ComponentId = componentId,
                Success = false,
                Error = "组件不存在"
            };
        }

        criteria ??= new ReplacementCriteria();

        var allComponents = _componentLibrary.List().Where(c => c.Id != componentId).ToList();
        var suggestions = new List<ReplacementSuggestion>();

        foreach (var candidate in allComponents)
        {
            var score = CalculateReplacementScore(component, candidate, criteria);
            if (score.TotalScore >= criteria.MinScore)
            {
                suggestions.Add(new ReplacementSuggestion
                {
                    ComponentId = candidate.Id,
                    ComponentName = candidate.Name,
                    Manufacturer = candidate.Manufacturer,
                    Model = candidate.Model,
                    Category = candidate.Category,
                    Score = score.TotalScore,
                    MatchDetails = score.MatchDetails,
                    Advantages = GetAdvantages(component, candidate),
                    Disadvantages = GetDisadvantages(component, candidate),
                    CompatibilityNotes = GetCompatibilityNotes(component, candidate)
                });
            }
        }

        // 按分数排序
        suggestions = suggestions.OrderByDescending(s => s.Score).ToList();

        return new ComponentReplacementResult
        {
            ComponentId = componentId,
            ComponentName = component.Name,
            Success = true,
            Suggestions = suggestions,
            Criteria = criteria
        };
    }

    /// <summary>
    /// 计算替代评分
    /// </summary>
    private ReplacementScore CalculateReplacementScore(
        ComponentLibraryService.ComponentRecord original,
        ComponentLibraryService.ComponentRecord candidate,
        ReplacementCriteria criteria)
    {
        var score = new ReplacementScore();
        var details = new List<string>();

        // 1. 类别匹配（30分）
        if (original.Category == candidate.Category)
        {
            score.CategoryMatch = 30;
            details.Add($"✓ 类别匹配: {candidate.Category}");
        }
        else
        {
            details.Add($"⚠ 类别不同: {original.Category} → {candidate.Category}");
        }

        // 2. 参数匹配（40分）
        var originalParams = GetComponentParameters(original);
        var candidateParams = GetComponentParameters(candidate);

        // MTTFd匹配（15分）
        if (originalParams.MTTFd > 0 && candidateParams.MTTFd > 0)
        {
            var ratio = Math.Min(originalParams.MTTFd, candidateParams.MTTFd) /
                       Math.Max(originalParams.MTTFd, candidateParams.MTTFd);
            score.ParameterMatch = (int)(ratio * 15);
            details.Add($"MTTFd匹配度: {ratio:P0}");
        }

        // DCavg匹配（15分）
        if (originalParams.DCavg > 0 && candidateParams.DCavg > 0)
        {
            var diff = Math.Abs(originalParams.DCavg - candidateParams.DCavg);
            score.ParameterMatch += (int)((1 - diff) * 15);
            details.Add($"DCavg差异: {diff:P2}");
        }

        // PFHd匹配（10分）
        if (originalParams.PFHd > 0 && candidateParams.PFHd > 0)
        {
            var ratio = Math.Min(originalParams.PFHd, candidateParams.PFHd) /
                       Math.Max(originalParams.PFHd, candidateParams.PFHd);
            score.ParameterMatch += (int)(ratio * 10);
            details.Add($"PFHd匹配度: {ratio:P0}");
        }

        // 3. 制造商匹配（10分）
        if (original.Manufacturer == candidate.Manufacturer)
        {
            score.ManufacturerMatch = 10;
            details.Add($"✓ 同制造商: {candidate.Manufacturer}");
        }

        // 4. 性能提升（20分）
        if (candidateParams.MTTFd > originalParams.MTTFd * 1.1)
        {
            score.PerformanceBonus = 10;
            details.Add($"✓ MTTFd提升: {originalParams.MTTFd} → {candidateParams.MTTFd}");
        }
        if (candidateParams.DCavg > originalParams.DCavg + 0.05)
        {
            score.PerformanceBonus += 10;
            details.Add($"✓ DCavg提升: {originalParams.DCavg:P2} → {candidateParams.DCavg:P2}");
        }

        score.MatchDetails = details;
        score.TotalScore = score.CategoryMatch + score.ParameterMatch + score.ManufacturerMatch + score.PerformanceBonus;

        return score;
    }

    /// <summary>
    /// 获取组件参数
    /// </summary>
    private ComponentParameters GetComponentParameters(ComponentLibraryService.ComponentRecord component)
    {
        var parameters = new ComponentParameters();

        if (component.Parameters != null)
        {
            var json = JsonSerializer.Serialize(component.Parameters);
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("MTTFd", out var mttfd))
                parameters.MTTFd = mttfd.GetDouble();

            if (doc.RootElement.TryGetProperty("DCavg", out var dcavg))
                parameters.DCavg = dcavg.GetDouble();

            if (doc.RootElement.TryGetProperty("PFHd", out var pfhd))
                parameters.PFHd = pfhd.GetDouble();
        }

        return parameters;
    }

    /// <summary>
    /// 获取优势
    /// </summary>
    private List<string> GetAdvantages(ComponentLibraryService.ComponentRecord original, ComponentLibraryService.ComponentRecord candidate)
    {
        var advantages = new List<string>();
        var originalParams = GetComponentParameters(original);
        var candidateParams = GetComponentParameters(candidate);

        if (candidateParams.MTTFd > originalParams.MTTFd)
            advantages.Add($"MTTFd更高: {candidateParams.MTTFd} > {originalParams.MTTFd}");

        if (candidateParams.DCavg > originalParams.DCavg)
            advantages.Add($"DCavg更高: {candidateParams.DCavg:P2} > {originalParams.DCavg:P2}");

        if (candidateParams.PFHd < originalParams.PFHd && originalParams.PFHd > 0)
            advantages.Add($"PFHd更低: {candidateParams.PFHd:E2} < {originalParams.PFHd:E2}");

        if (candidate.Manufacturer == original.Manufacturer)
            advantages.Add("同制造商，兼容性更好");

        return advantages;
    }

    /// <summary>
    /// 获取劣势
    /// </summary>
    private List<string> GetDisadvantages(ComponentLibraryService.ComponentRecord original, ComponentLibraryService.ComponentRecord candidate)
    {
        var disadvantages = new List<string>();
        var originalParams = GetComponentParameters(original);
        var candidateParams = GetComponentParameters(candidate);

        if (candidateParams.MTTFd < originalParams.MTTFd)
            disadvantages.Add($"MTTFd较低: {candidateParams.MTTFd} < {originalParams.MTTFd}");

        if (candidateParams.DCavg < originalParams.DCavg)
            disadvantages.Add($"DCavg较低: {candidateParams.DCavg:P2} < {originalParams.DCavg:P2}");

        if (candidateParams.PFHd > originalParams.PFHd && originalParams.PFHd > 0)
            disadvantages.Add($"PFHd较高: {candidateParams.PFHd:E2} > {originalParams.PFHd:E2}");

        if (candidate.Category != original.Category)
            disadvantages.Add($"类别不同，可能需要重新评估");

        return disadvantages;
    }

    /// <summary>
    /// 获取兼容性说明
    /// </summary>
    private List<string> GetCompatibilityNotes(ComponentLibraryService.ComponentRecord original, ComponentLibraryService.ComponentRecord candidate)
    {
        var notes = new List<string>();
        var originalParams = GetComponentParameters(original);
        var candidateParams = GetComponentParameters(candidate);

        if (candidate.Category != original.Category)
            notes.Add("类别不同，需要重新进行安全评估");

        if (Math.Abs(candidateParams.DCavg - originalParams.DCavg) > 0.1)
            notes.Add("DCavg差异较大，可能影响系统性能等级");

        if (candidateParams.MTTFd < originalParams.MTTFd * 0.8)
            notes.Add("MTTFd显著降低，可能影响系统可靠性");

        if (candidate.Manufacturer != original.Manufacturer)
            notes.Add("不同制造商，需要验证接口兼容性");

        return notes;
    }
}

public class ComponentReplacementResult
{
    public string ComponentId { get; set; } = string.Empty;
    public string? ComponentName { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<ReplacementSuggestion> Suggestions { get; set; } = new();
    public ReplacementCriteria? Criteria { get; set; }
}

public class ReplacementSuggestion
{
    public string ComponentId { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Score { get; set; }
    public List<string> MatchDetails { get; set; } = new();
    public List<string> Advantages { get; set; } = new();
    public List<string> Disadvantages { get; set; } = new();
    public List<string> CompatibilityNotes { get; set; } = new();
}

public class ReplacementCriteria
{
    public int MinScore { get; set; } = 50;
    public bool RequireSameCategory { get; set; } = false;
    public bool RequireSameManufacturer { get; set; } = false;
    public double? MinMTTFd { get; set; }
    public double? MinDCavg { get; set; }
    public double? MaxPFHd { get; set; }
}

public class ReplacementScore
{
    public int CategoryMatch { get; set; }
    public int ParameterMatch { get; set; }
    public int ManufacturerMatch { get; set; }
    public int PerformanceBonus { get; set; }
    public int TotalScore => CategoryMatch + ParameterMatch + ManufacturerMatch + PerformanceBonus;
    public List<string> MatchDetails { get; set; } = new();
}

public class ComponentParameters
{
    public double MTTFd { get; set; }
    public double DCavg { get; set; }
    public double PFHd { get; set; }
}

