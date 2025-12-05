using System.Collections.Concurrent;

namespace SafeTool.Application.Services;

/// <summary>
/// 缓存管理服务（P2优先级）
/// 管理系统缓存
/// </summary>
public class CacheManagementService
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
    private readonly System.Timers.Timer _cleanupTimer;

    public CacheManagementService()
    {
        _cleanupTimer = new System.Timers.Timer(60000); // 每分钟清理一次
        _cleanupTimer.Elapsed += (sender, e) => CleanupExpiredItems();
        _cleanupTimer.Start();
    }

    /// <summary>
    /// 设置缓存项
    /// </summary>
    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var item = new CacheItem
        {
            Key = key,
            Value = value!,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null
        };

        _cache.AddOrUpdate(key, item, (k, v) => item);
    }

    /// <summary>
    /// 获取缓存项
    /// </summary>
    public T? Get<T>(string key)
    {
        if (!_cache.TryGetValue(key, out var item))
            return default;

        // 检查是否过期
        if (item.ExpiresAt.HasValue && item.ExpiresAt.Value < DateTime.UtcNow)
        {
            _cache.TryRemove(key, out _);
            return default;
        }

        return item.Value is T ? (T)item.Value : default;
    }

    /// <summary>
    /// 删除缓存项
    /// </summary>
    public bool Remove(string key)
    {
        return _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// 获取缓存统计
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        var now = DateTime.UtcNow;
        var items = _cache.Values.ToList();

        return new CacheStatistics
        {
            TotalItems = items.Count,
            ExpiredItems = items.Count(i => i.ExpiresAt.HasValue && i.ExpiresAt.Value < now),
            ValidItems = items.Count(i => !i.ExpiresAt.HasValue || i.ExpiresAt.Value >= now),
            TotalSize = EstimateSize(items),
            OldestItem = items.OrderBy(i => i.CreatedAt).FirstOrDefault()?.CreatedAt,
            NewestItem = items.OrderByDescending(i => i.CreatedAt).FirstOrDefault()?.CreatedAt
        };
    }

    /// <summary>
    /// 清理过期项
    /// </summary>
    private void CleanupExpiredItems()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value < now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// 估算缓存大小
    /// </summary>
    private long EstimateSize(List<CacheItem> items)
    {
        // 简化的估算，实际应更精确
        return items.Count * 1024; // 假设每个项约1KB
    }
}

public class CacheItem
{
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class CacheStatistics
{
    public int TotalItems { get; set; }
    public int ExpiredItems { get; set; }
    public int ValidItems { get; set; }
    public long TotalSize { get; set; }
    public DateTime? OldestItem { get; set; }
    public DateTime? NewestItem { get; set; }
}

