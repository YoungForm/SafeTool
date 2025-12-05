using SafeTool.Application.Repositories;
using SafeTool.Domain.ChangeManagement;

namespace SafeTool.Application.Services;

/// <summary>
/// 变更请求服务（应用服务层，使用Repository模式）
/// </summary>
public class ChangeRequestService
{
    private readonly IChangeRequestRepository _repository;
    private readonly AuditService _auditService;
    private readonly IChangeImpactAnalyzer _impactAnalyzer;
    private readonly IVersionComparer _versionComparer;

    public ChangeRequestService(
        IChangeRequestRepository repository,
        AuditService auditService,
        IChangeImpactAnalyzer impactAnalyzer,
        IVersionComparer versionComparer)
    {
        _repository = repository;
        _auditService = auditService;
        _impactAnalyzer = impactAnalyzer;
        _versionComparer = versionComparer;
    }

    /// <summary>
    /// 创建变更请求
    /// </summary>
    public async Task<ChangeRequest> CreateAsync(ChangeRequest changeRequest, string requester)
    {
        changeRequest.Requester = requester;
        changeRequest.CreatedAt = DateTime.UtcNow;
        
        // 执行影响分析
        var impact = await _impactAnalyzer.AnalyzeAsync(changeRequest);
        changeRequest.ImpactAnalysis = impact.Analysis;
        changeRequest.AffectedItems = impact.AffectedItems;
        changeRequest.RequiresReEvaluation = impact.RequiresReEvaluation;
        changeRequest.RequiresReVerification = impact.RequiresReVerification;
        
        var created = await _repository.CreateAsync(changeRequest);
        _auditService.Log(requester, "create", "changerequest", $"创建变更请求: {changeRequest.Id}");
        
        return created;
    }

    /// <summary>
    /// 提交变更请求
    /// </summary>
    public async Task<ChangeRequest> SubmitAsync(string id, string requester)
    {
        var cr = await _repository.GetByIdAsync(id);
        if (cr == null)
            throw new KeyNotFoundException($"变更请求 {id} 不存在");
        
        cr.Submit(requester);
        var updated = await _repository.UpdateAsync(cr);
        _auditService.Log(requester, "submit", "changerequest", $"提交变更请求: {id}");
        
        return updated;
    }

    /// <summary>
    /// 审批变更请求
    /// </summary>
    public async Task<ChangeRequest> ApproveAsync(string id, string reviewer, string comment, bool isFirstReviewer)
    {
        var cr = await _repository.GetByIdAsync(id);
        if (cr == null)
            throw new KeyNotFoundException($"变更请求 {id} 不存在");
        
        cr.Approve(reviewer, comment, isFirstReviewer);
        var updated = await _repository.UpdateAsync(cr);
        _auditService.Log(reviewer, "approve", "changerequest", $"审批变更请求: {id}");
        
        return updated;
    }

    /// <summary>
    /// 拒绝变更请求
    /// </summary>
    public async Task<ChangeRequest> RejectAsync(string id, string reviewer, string reason)
    {
        var cr = await _repository.GetByIdAsync(id);
        if (cr == null)
            throw new KeyNotFoundException($"变更请求 {id} 不存在");
        
        cr.Reject(reviewer, reason);
        var updated = await _repository.UpdateAsync(cr);
        _auditService.Log(reviewer, "reject", "changerequest", $"拒绝变更请求: {id}, 原因: {reason}");
        
        return updated;
    }

    /// <summary>
    /// 实施变更
    /// </summary>
    public async Task<ChangeRequest> ImplementAsync(string id, string implementer)
    {
        var cr = await _repository.GetByIdAsync(id);
        if (cr == null)
            throw new KeyNotFoundException($"变更请求 {id} 不存在");
        
        cr.Implement(implementer);
        var updated = await _repository.UpdateAsync(cr);
        _auditService.Log(implementer, "implement", "changerequest", $"实施变更请求: {id}");
        
        return updated;
    }

    /// <summary>
    /// 生成版本对比
    /// </summary>
    public async Task<string> GenerateVersionDiffAsync(string id)
    {
        var cr = await _repository.GetByIdAsync(id);
        if (cr == null)
            throw new KeyNotFoundException($"变更请求 {id} 不存在");
        
        if (string.IsNullOrWhiteSpace(cr.PreviousVersionId) || string.IsNullOrWhiteSpace(cr.NewVersionId))
            return "无法生成版本对比：缺少版本ID";
        
        var diff = await _versionComparer.CompareAsync(cr.PreviousVersionId, cr.NewVersionId, cr.AffectedResourceType);
        cr.VersionDiff = diff;
        await _repository.UpdateAsync(cr);
        
        return diff;
    }

    /// <summary>
    /// 查询变更请求
    /// </summary>
    public async Task<IEnumerable<ChangeRequest>> QueryAsync(string? projectId, ChangeStatus? status, string? requester)
    {
        if (!string.IsNullOrWhiteSpace(projectId))
            return await _repository.GetByProjectIdAsync(projectId);
        
        if (status.HasValue)
            return await _repository.GetByStatusAsync(status.Value);
        
        if (!string.IsNullOrWhiteSpace(requester))
            return await _repository.GetByRequesterAsync(requester);
        
        return Enumerable.Empty<ChangeRequest>();
    }
}

/// <summary>
/// 变更影响分析器接口（策略模式）
/// </summary>
public interface IChangeImpactAnalyzer
{
    Task<ImpactAnalysisResult> AnalyzeAsync(ChangeRequest changeRequest);
}

/// <summary>
/// 变更影响分析器实现
/// </summary>
public class ChangeImpactAnalyzer : IChangeImpactAnalyzer
{
    public Task<ImpactAnalysisResult> AnalyzeAsync(ChangeRequest changeRequest)
    {
        var result = new ImpactAnalysisResult
        {
            Analysis = $"变更类型: {changeRequest.Type}, 优先级: {changeRequest.Priority}",
            AffectedItems = new List<string>(),
            RequiresReEvaluation = false,
            RequiresReVerification = false
        };
        
        // 根据变更类型判断影响
        switch (changeRequest.Type)
        {
            case ChangeType.SRSUpdate:
            case ChangeType.FunctionModify:
            case ChangeType.ParameterAdjust:
                result.RequiresReEvaluation = true;
                result.RequiresReVerification = true;
                result.Analysis += "\n影响：需要重新评估和验证";
                break;
            
            case ChangeType.ComponentChange:
                result.RequiresReEvaluation = true;
                result.Analysis += "\n影响：组件变更需要重新评估";
                break;
            
            case ChangeType.EvidenceUpdate:
                result.RequiresReVerification = true;
                result.Analysis += "\n影响：证据更新需要重新验证";
                break;
        }
        
        return Task.FromResult(result);
    }
}

public class ImpactAnalysisResult
{
    public string Analysis { get; set; } = string.Empty;
    public List<string> AffectedItems { get; set; } = new();
    public bool RequiresReEvaluation { get; set; }
    public bool RequiresReVerification { get; set; }
}

/// <summary>
/// 版本对比器接口（策略模式）
/// </summary>
public interface IVersionComparer
{
    Task<string> CompareAsync(string previousVersionId, string newVersionId, string resourceType);
}

/// <summary>
/// 版本对比器实现
/// </summary>
public class VersionComparer : IVersionComparer
{
    public Task<string> CompareAsync(string previousVersionId, string newVersionId, string resourceType)
    {
        // 简化实现：实际应该从版本库中获取并对比
        var diff = $@"版本对比结果：
资源类型: {resourceType}
旧版本ID: {previousVersionId}
新版本ID: {newVersionId}
对比时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}

[实际实现中应从版本库获取详细差异]";
        
        return Task.FromResult(diff);
    }
}

