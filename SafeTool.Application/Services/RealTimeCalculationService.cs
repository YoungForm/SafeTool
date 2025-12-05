using System.Collections.Concurrent;

namespace SafeTool.Application.Services;

/// <summary>
/// 实时计算反馈服务（观察者模式）
/// </summary>
public class RealTimeCalculationService
{
    private readonly ConcurrentDictionary<string, CalculationSession> _sessions = new();

    /// <summary>
    /// 创建计算会话
    /// </summary>
    public string CreateSession(string? sessionId = null)
    {
        var id = sessionId ?? Guid.NewGuid().ToString("N");
        _sessions[id] = new CalculationSession
        {
            Id = id,
            CreatedAt = DateTime.UtcNow,
            Status = CalculationStatus.Pending,
            Progress = 0,
            Results = new List<CalculationResult>(),
            Messages = new List<CalculationMessage>()
        };
        return id;
    }

    /// <summary>
    /// 执行实时计算
    /// </summary>
    public async Task<CalculationSession> ExecuteCalculationAsync(
        string sessionId,
        CalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            throw new KeyNotFoundException($"会话 {sessionId} 不存在");
        }

        session.Status = CalculationStatus.Running;
        session.Progress = 0;
        session.Messages.Clear();
        session.Results.Clear();

        try
        {
            // 模拟分步计算
            await UpdateProgress(session, 10, "开始计算...", cancellationToken);

            // 步骤1：验证输入
            await UpdateProgress(session, 20, "验证输入参数...", cancellationToken);
            await Task.Delay(100, cancellationToken);

            // 步骤2：执行计算
            await UpdateProgress(session, 50, "执行计算...", cancellationToken);
            await Task.Delay(200, cancellationToken);

            // 步骤3：生成结果
            await UpdateProgress(session, 80, "生成结果...", cancellationToken);
            await Task.Delay(100, cancellationToken);

            // 步骤4：完成
            await UpdateProgress(session, 100, "计算完成", cancellationToken);

            session.Status = CalculationStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;

            // 添加计算结果
            session.Results.Add(new CalculationResult
            {
                Type = request.CalculationType,
                Data = request.InputData,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (OperationCanceledException)
        {
            session.Status = CalculationStatus.Cancelled;
            session.Messages.Add(new CalculationMessage
            {
                Level = MessageLevel.Warning,
                Message = "计算已取消"
            });
        }
        catch (Exception ex)
        {
            session.Status = CalculationStatus.Failed;
            session.Messages.Add(new CalculationMessage
            {
                Level = MessageLevel.Error,
                Message = $"计算失败: {ex.Message}"
            });
        }

        return session;
    }

    /// <summary>
    /// 获取计算会话状态
    /// </summary>
    public CalculationSession? GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }

    /// <summary>
    /// 取消计算
    /// </summary>
    public bool CancelSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Status = CalculationStatus.Cancelled;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 清理过期会话
    /// </summary>
    public void CleanupExpiredSessions(TimeSpan maxAge)
    {
        var expired = _sessions
            .Where(kvp => DateTime.UtcNow - kvp.Value.CreatedAt > maxAge)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in expired)
        {
            _sessions.TryRemove(id, out _);
        }
    }

    private async Task UpdateProgress(
        CalculationSession session,
        int progress,
        string message,
        CancellationToken cancellationToken)
    {
        session.Progress = progress;
        session.Messages.Add(new CalculationMessage
        {
            Level = MessageLevel.Info,
            Message = message,
            Timestamp = DateTime.UtcNow
        });

        // 模拟异步操作
        await Task.Delay(50, cancellationToken);
    }
}

public class CalculationSession
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public CalculationStatus Status { get; set; }
    public int Progress { get; set; } // 0-100
    public List<CalculationResult> Results { get; set; } = new();
    public List<CalculationMessage> Messages { get; set; } = new();
}

public enum CalculationStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public class CalculationRequest
{
    public string CalculationType { get; set; } = string.Empty; // "dcavg", "pfhd", "pl", "sil"
    public Dictionary<string, object> InputData { get; set; } = new();
}

public class CalculationResult
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class CalculationMessage
{
    public MessageLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public enum MessageLevel
{
    Info,
    Warning,
    Error
}

