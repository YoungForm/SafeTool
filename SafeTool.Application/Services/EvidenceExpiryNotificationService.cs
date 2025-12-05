using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SafeTool.Application.Repositories;
using SafeTool.Application.Services;

namespace SafeTool.Application.Services;

/// <summary>
/// 证据到期提醒服务（观察者模式 + 后台任务）
/// </summary>
public class EvidenceExpiryNotificationService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EvidenceExpiryNotificationService> _logger;
    private Timer? _timer;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // 每天检查一次
    private readonly int _warningDays = 30; // 提前30天提醒

    public EvidenceExpiryNotificationService(
        IServiceProvider serviceProvider,
        ILogger<EvidenceExpiryNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("证据到期提醒服务已启动");
        _timer = new Timer(DoWork, null, TimeSpan.Zero, _checkInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("证据到期提醒服务已停止");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var evidenceService = scope.ServiceProvider.GetRequiredService<EvidenceService>();
            var notificationService = scope.ServiceProvider.GetService<INotificationService>();
            
            var allEvidence = evidenceService.List(null, null);
            var now = DateTime.UtcNow;
            var warningDate = now.AddDays(_warningDays);
            
            var expiringSoon = allEvidence
                .Where(e => e.ValidUntil.HasValue && 
                           e.ValidUntil.Value <= warningDate && 
                           e.ValidUntil.Value > now)
                .ToList();
            
            var expired = allEvidence
                .Where(e => e.ValidUntil.HasValue && e.ValidUntil.Value <= now)
                .ToList();
            
            if (expiringSoon.Any() || expired.Any())
            {
                _logger.LogWarning("发现 {ExpiringCount} 个即将到期和 {ExpiredCount} 个已过期的证据",
                    expiringSoon.Count, expired.Count);
                
                if (notificationService != null)
                {
                    foreach (var evidence in expiringSoon)
                    {
                        await notificationService.NotifyAsync(new Notification
                        {
                            Type = NotificationType.Warning,
                            Title = "证据即将到期",
                            Message = $"证据 {evidence.Name} (ID: {evidence.Id}) 将在 {evidence.ValidUntil!.Value:yyyy-MM-dd} 到期",
                            ResourceType = "Evidence",
                            ResourceId = evidence.Id,
                            CreatedAt = now
                        });
                    }
                    
                    foreach (var evidence in expired)
                    {
                        await notificationService.NotifyAsync(new Notification
                        {
                            Type = NotificationType.Error,
                            Title = "证据已过期",
                            Message = $"证据 {evidence.Name} (ID: {evidence.Id}) 已于 {evidence.ValidUntil!.Value:yyyy-MM-dd} 过期",
                            ResourceType = "Evidence",
                            ResourceId = evidence.Id,
                            CreatedAt = now
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查证据到期时发生错误");
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

/// <summary>
/// 通知服务接口（观察者模式）
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(Notification notification);
    Task<IEnumerable<Notification>> GetNotificationsAsync(string? userId, NotificationType? type);
}

/// <summary>
/// 通知服务实现
/// </summary>
public class NotificationService : INotificationService
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private List<Notification> _notifications = new();

    public NotificationService(string dataDir)
    {
        var dir = Path.Combine(dataDir, "Notifications");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "notifications.json");
        Load();
    }

    private void Load()
    {
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            var data = System.Text.Json.JsonSerializer.Deserialize<List<Notification>>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data != null)
                _notifications = data;
        }
    }

    private void Save()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_notifications, 
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    public Task NotifyAsync(Notification notification)
    {
        lock (_lock)
        {
            _notifications.Add(notification);
            Save();
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Notification>> GetNotificationsAsync(string? userId, NotificationType? type)
    {
        lock (_lock)
        {
            var query = _notifications.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(n => n.UserId == userId);
            
            if (type.HasValue)
                query = query.Where(n => n.Type == type.Value);
            
            return Task.FromResult<IEnumerable<Notification>>(query.OrderByDescending(n => n.CreatedAt).ToList());
        }
    }
}

public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string? UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    Info,
    Warning,
    Error,
    Success
}

