using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 整改项闭环跟踪服务（状态机模式）
/// </summary>
public class RemediationTrackingService
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private Dictionary<string, List<RemediationItem>> _remediations = new();

    public RemediationTrackingService(string dataDir)
    {
        var dir = Path.Combine(dataDir, "Remediation");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "remediations.json");
        Load();
    }

    private void Load()
    {
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, List<RemediationItem>>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data != null)
                _remediations = data;
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_remediations,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    /// <summary>
    /// 创建整改项
    /// </summary>
    public RemediationItem CreateRemediation(string projectId, RemediationItem item)
    {
        item.Id = item.Id ?? Guid.NewGuid().ToString("N");
        item.ProjectId = projectId;
        item.Status = RemediationStatus.Open;
        item.CreatedAt = DateTime.UtcNow;

        lock (_lock)
        {
            if (!_remediations.TryGetValue(projectId, out var list))
            {
                list = new List<RemediationItem>();
                _remediations[projectId] = list;
            }
            list.Add(item);
            Save();
        }

        return item;
    }

    /// <summary>
    /// 更新整改项状态（状态机）
    /// </summary>
    public RemediationItem UpdateStatus(string projectId, string itemId, RemediationStatus newStatus, string? updatedBy = null, string? comment = null)
    {
        lock (_lock)
        {
            if (!_remediations.TryGetValue(projectId, out var list))
                throw new KeyNotFoundException($"项目 {projectId} 不存在");

            var item = list.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                throw new KeyNotFoundException($"整改项 {itemId} 不存在");

            // 状态机转换验证
            if (!IsValidTransition(item.Status, newStatus))
                throw new InvalidOperationException($"不能从状态 {item.Status} 转换到 {newStatus}");

            // 更新状态
            item.PreviousStatus = item.Status;
            item.Status = newStatus;
            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedBy = updatedBy;

            // 记录状态变更历史
            item.StatusHistory.Add(new StatusChange
            {
                FromStatus = item.PreviousStatus ?? RemediationStatus.Open,
                ToStatus = newStatus,
                ChangedBy = updatedBy ?? "system",
                ChangedAt = DateTime.UtcNow,
                Comment = comment
            });

            // 根据状态设置时间戳
            switch (newStatus)
            {
                case RemediationStatus.InProgress:
                    item.StartedAt ??= DateTime.UtcNow;
                    break;
                case RemediationStatus.Completed:
                    item.CompletedAt = DateTime.UtcNow;
                    break;
                case RemediationStatus.Closed:
                    item.ClosedAt = DateTime.UtcNow;
                    break;
            }

            Save();
            return item;
        }
    }

    /// <summary>
    /// 分配责任人
    /// </summary>
    public RemediationItem AssignOwner(string projectId, string itemId, string owner, string? assignedBy = null)
    {
        lock (_lock)
        {
            if (!_remediations.TryGetValue(projectId, out var list))
                throw new KeyNotFoundException($"项目 {projectId} 不存在");

            var item = list.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                throw new KeyNotFoundException($"整改项 {itemId} 不存在");

            item.Owner = owner;
            item.AssignedAt = DateTime.UtcNow;
            item.AssignedBy = assignedBy;

            // 如果状态是Open，自动转为InProgress
            if (item.Status == RemediationStatus.Open)
            {
                UpdateStatus(projectId, itemId, RemediationStatus.InProgress, assignedBy, "责任人已分配，开始整改");
            }

            Save();
            return item;
        }
    }

    /// <summary>
    /// 验证状态转换是否有效
    /// </summary>
    private bool IsValidTransition(RemediationStatus from, RemediationStatus to)
    {
        return (from, to) switch
        {
            (RemediationStatus.Open, RemediationStatus.InProgress) => true,
            (RemediationStatus.Open, RemediationStatus.Cancelled) => true,
            (RemediationStatus.InProgress, RemediationStatus.Completed) => true,
            (RemediationStatus.InProgress, RemediationStatus.OnHold) => true,
            (RemediationStatus.InProgress, RemediationStatus.Cancelled) => true,
            (RemediationStatus.OnHold, RemediationStatus.InProgress) => true,
            (RemediationStatus.OnHold, RemediationStatus.Cancelled) => true,
            (RemediationStatus.Completed, RemediationStatus.Closed) => true,
            (RemediationStatus.Completed, RemediationStatus.Reopened) => true,
            (RemediationStatus.Reopened, RemediationStatus.InProgress) => true,
            (RemediationStatus.Reopened, RemediationStatus.Cancelled) => true,
            _ => false
        };
    }

    /// <summary>
    /// 获取项目的所有整改项
    /// </summary>
    public IEnumerable<RemediationItem> GetRemediations(string projectId, RemediationStatus? status = null, string? owner = null)
    {
        lock (_lock)
        {
            if (!_remediations.TryGetValue(projectId, out var list))
                return Enumerable.Empty<RemediationItem>();

            var query = list.AsEnumerable();
            if (status.HasValue)
                query = query.Where(i => i.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(owner))
                query = query.Where(i => i.Owner == owner);

            return query.OrderByDescending(i => i.CreatedAt);
        }
    }

    /// <summary>
    /// 获取超期整改项
    /// </summary>
    public IEnumerable<RemediationItem> GetOverdueRemediations(string projectId)
    {
        var now = DateTime.UtcNow;
        return GetRemediations(projectId)
            .Where(i => i.DueDate.HasValue && 
                       i.DueDate.Value < now && 
                       i.Status != RemediationStatus.Completed && 
                       i.Status != RemediationStatus.Closed);
    }

    /// <summary>
    /// 完成整改并关联证据
    /// </summary>
    public RemediationItem CompleteRemediation(string projectId, string itemId, string? evidenceId, string? completedBy = null, string? comment = null)
    {
        var item = UpdateStatus(projectId, itemId, RemediationStatus.Completed, completedBy, comment);
        
        if (!string.IsNullOrWhiteSpace(evidenceId))
        {
            lock (_lock)
            {
                item.EvidenceId = evidenceId;
                Save();
            }
        }

        return item;
    }
}

public class RemediationItem
{
    public string Id { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Standard { get; set; }
    public string? Clause { get; set; }
    public string? ReferenceItemId { get; set; } // 关联的验证清单项ID
    public RemediationStatus Status { get; set; }
    public RemediationStatus? PreviousStatus { get; set; }
    public string? Owner { get; set; }
    public string? AssignedBy { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = "Medium"; // Low/Medium/High/Critical
    public string? EvidenceId { get; set; }
    public List<StatusChange> StatusHistory { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public enum RemediationStatus
{
    Open,        // 待处理
    InProgress,  // 进行中
    OnHold,      // 暂停
    Completed,   // 已完成
    Closed,      // 已关闭
    Reopened,    // 已重新打开
    Cancelled    // 已取消
}

public class StatusChange
{
    public RemediationStatus FromStatus { get; set; }
    public RemediationStatus ToStatus { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Comment { get; set; }
}

