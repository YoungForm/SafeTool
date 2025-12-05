namespace SafeTool.Domain.ChangeManagement;

/// <summary>
/// 变更请求领域模型（领域驱动设计）
/// </summary>
public class ChangeRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProjectId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ChangeType Type { get; set; }
    public ChangePriority Priority { get; set; } = ChangePriority.Medium;
    public ChangeStatus Status { get; set; } = ChangeStatus.Draft;
    
    // 变更内容
    public string AffectedResourceType { get; set; } = string.Empty; // SRS/Function/Component/Evidence
    public string AffectedResourceId { get; set; } = string.Empty;
    public string ChangeDetails { get; set; } = string.Empty; // JSON格式存储变更详情
    
    // 影响分析
    public string ImpactAnalysis { get; set; } = string.Empty;
    public List<string> AffectedItems { get; set; } = new(); // 受影响的评估/验证/报告ID列表
    public bool RequiresReEvaluation { get; set; }
    public bool RequiresReVerification { get; set; }
    
    // 审批流程
    public string Requester { get; set; } = string.Empty;
    public string? Reviewer1 { get; set; }
    public string? Reviewer2 { get; set; }
    public DateTime? ReviewedAt1 { get; set; }
    public DateTime? ReviewedAt2 { get; set; }
    public string? ReviewComment1 { get; set; }
    public string? ReviewComment2 { get; set; }
    public bool IsDualReviewRequired { get; set; } = true;
    
    // 版本对比
    public string? PreviousVersionId { get; set; }
    public string? NewVersionId { get; set; }
    public string? VersionDiff { get; set; } // JSON格式存储差异
    
    // 时间戳
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ImplementedAt { get; set; }
    
    // 审计
    public List<ChangeEvent> Events { get; set; } = new();
    
    /// <summary>
    /// 提交变更请求
    /// </summary>
    public void Submit(string requester)
    {
        if (Status != ChangeStatus.Draft)
            throw new InvalidOperationException("只能提交草稿状态的变更请求");
        
        Requester = requester;
        Status = ChangeStatus.Submitted;
        Events.Add(new ChangeEvent
        {
            Timestamp = DateTime.UtcNow,
            User = requester,
            Action = "Submit",
            Description = "提交变更请求"
        });
    }
    
    /// <summary>
    /// 审批变更请求
    /// </summary>
    public void Approve(string reviewer, string comment, bool isFirstReviewer)
    {
        if (Status != ChangeStatus.Submitted && Status != ChangeStatus.UnderReview)
            throw new InvalidOperationException("只能审批已提交的变更请求");
        
        if (isFirstReviewer)
        {
            Reviewer1 = reviewer;
            ReviewedAt1 = DateTime.UtcNow;
            ReviewComment1 = comment;
            
            if (IsDualReviewRequired)
            {
                Status = ChangeStatus.UnderReview;
            }
            else
            {
                Status = ChangeStatus.Approved;
                ApprovedAt = DateTime.UtcNow;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Reviewer1))
                throw new InvalidOperationException("需要先完成第一人审批");
            
            Reviewer2 = reviewer;
            ReviewedAt2 = DateTime.UtcNow;
            ReviewComment2 = comment;
            Status = ChangeStatus.Approved;
            ApprovedAt = DateTime.UtcNow;
        }
        
        Events.Add(new ChangeEvent
        {
            Timestamp = DateTime.UtcNow,
            User = reviewer,
            Action = "Approve",
            Description = $"审批变更请求：{comment}"
        });
    }
    
    /// <summary>
    /// 拒绝变更请求
    /// </summary>
    public void Reject(string reviewer, string reason)
    {
        if (Status != ChangeStatus.Submitted && Status != ChangeStatus.UnderReview)
            throw new InvalidOperationException("只能拒绝已提交的变更请求");
        
        Status = ChangeStatus.Rejected;
        Events.Add(new ChangeEvent
        {
            Timestamp = DateTime.UtcNow,
            User = reviewer,
            Action = "Reject",
            Description = $"拒绝变更请求：{reason}"
        });
    }
    
    /// <summary>
    /// 实施变更
    /// </summary>
    public void Implement(string implementer)
    {
        if (Status != ChangeStatus.Approved)
            throw new InvalidOperationException("只能实施已审批的变更请求");
        
        Status = ChangeStatus.Implemented;
        ImplementedAt = DateTime.UtcNow;
        Events.Add(new ChangeEvent
        {
            Timestamp = DateTime.UtcNow,
            User = implementer,
            Action = "Implement",
            Description = "实施变更"
        });
    }
}

public enum ChangeType
{
    SRSUpdate,      // SRS更新
    FunctionModify, // 安全功能修改
    ComponentChange, // 组件变更
    ParameterAdjust, // 参数调整
    EvidenceUpdate,  // 证据更新
    Other           // 其他
}

public enum ChangePriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum ChangeStatus
{
    Draft,          // 草稿
    Submitted,      // 已提交
    UnderReview,    // 审批中
    Approved,       // 已审批
    Rejected,       // 已拒绝
    Implemented     // 已实施
}

public class ChangeEvent
{
    public DateTime Timestamp { get; set; }
    public string User { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

