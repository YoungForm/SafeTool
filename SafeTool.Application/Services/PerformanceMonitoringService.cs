using System.Collections.Concurrent;
using System.Diagnostics;

namespace SafeTool.Application.Services;

/// <summary>
/// 性能监控服务（P2优先级）
/// 监控系统性能指标
/// </summary>
public class PerformanceMonitoringService
{
    private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics = new();
    private readonly object _lock = new();

    /// <summary>
    /// 记录性能指标
    /// </summary>
    public void RecordMetric(string operation, TimeSpan duration, bool success = true)
    {
        var metric = _metrics.GetOrAdd(operation, _ => new PerformanceMetric
        {
            Operation = operation,
            FirstRecordedAt = DateTime.UtcNow
        });

        lock (_lock)
        {
            metric.TotalCount++;
            metric.TotalDuration += duration.TotalMilliseconds;
            metric.SuccessCount += success ? 1 : 0;
            metric.FailureCount += success ? 0 : 1;
            metric.LastRecordedAt = DateTime.UtcNow;

            // 更新统计
            metric.AverageDuration = metric.TotalDuration / metric.TotalCount;
            metric.SuccessRate = (double)metric.SuccessCount / metric.TotalCount * 100;

            // 更新最小/最大持续时间
            if (duration.TotalMilliseconds < metric.MinDuration || metric.MinDuration == 0)
                metric.MinDuration = duration.TotalMilliseconds;
            if (duration.TotalMilliseconds > metric.MaxDuration)
                metric.MaxDuration = duration.TotalMilliseconds;
        }
    }

    /// <summary>
    /// 获取性能指标
    /// </summary>
    public PerformanceMetric? GetMetric(string operation)
    {
        return _metrics.TryGetValue(operation, out var metric) ? metric : null;
    }

    /// <summary>
    /// 获取所有性能指标
    /// </summary>
    public List<PerformanceMetric> GetAllMetrics()
    {
        return _metrics.Values.OrderByDescending(m => m.TotalCount).ToList();
    }

    /// <summary>
    /// 获取性能报告
    /// </summary>
    public PerformanceReport GenerateReport(DateTime? from = null, DateTime? to = null)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
        var toDate = to ?? DateTime.UtcNow;

        var metrics = _metrics.Values
            .Where(m => m.LastRecordedAt >= fromDate && m.LastRecordedAt <= toDate)
            .ToList();

        return new PerformanceReport
        {
            GeneratedAt = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            TotalOperations = metrics.Sum(m => m.TotalCount),
            TotalSuccess = metrics.Sum(m => m.SuccessCount),
            TotalFailures = metrics.Sum(m => m.FailureCount),
            AverageDuration = metrics.Any() ? metrics.Average(m => m.AverageDuration) : 0,
            SlowestOperations = metrics.OrderByDescending(m => m.MaxDuration).Take(10).ToList(),
            MostFrequentOperations = metrics.OrderByDescending(m => m.TotalCount).Take(10).ToList(),
            OperationsBySuccessRate = metrics.OrderByDescending(m => m.SuccessRate).ToList()
        };
    }

    /// <summary>
    /// 重置指标
    /// </summary>
    public void ResetMetrics(string? operation = null)
    {
        if (string.IsNullOrEmpty(operation))
        {
            _metrics.Clear();
        }
        else
        {
            _metrics.TryRemove(operation, out _);
        }
    }

    /// <summary>
    /// 检查性能警告
    /// </summary>
    public List<PerformanceWarning> CheckPerformanceWarnings()
    {
        var warnings = new List<PerformanceWarning>();

        foreach (var metric in _metrics.Values)
        {
            // 检查平均响应时间
            if (metric.AverageDuration > 5000) // 5秒
            {
                warnings.Add(new PerformanceWarning
                {
                    Operation = metric.Operation,
                    Type = "SlowAverage",
                    Message = $"操作 {metric.Operation} 平均响应时间过长: {metric.AverageDuration:F2}ms",
                    Severity = "High"
                });
            }

            // 检查最大响应时间
            if (metric.MaxDuration > 10000) // 10秒
            {
                warnings.Add(new PerformanceWarning
                {
                    Operation = metric.Operation,
                    Type = "SlowMax",
                    Message = $"操作 {metric.Operation} 最大响应时间过长: {metric.MaxDuration:F2}ms",
                    Severity = "High"
                });
            }

            // 检查失败率
            if (metric.SuccessRate < 95 && metric.TotalCount > 10)
            {
                warnings.Add(new PerformanceWarning
                {
                    Operation = metric.Operation,
                    Type = "HighFailureRate",
                    Message = $"操作 {metric.Operation} 失败率过高: {100 - metric.SuccessRate:F2}%",
                    Severity = "Medium"
                });
            }
        }

        return warnings;
    }
}

public class PerformanceMetric
{
    public string Operation { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double TotalDuration { get; set; }
    public double AverageDuration { get; set; }
    public double MinDuration { get; set; }
    public double MaxDuration { get; set; }
    public double SuccessRate { get; set; }
    public DateTime FirstRecordedAt { get; set; }
    public DateTime LastRecordedAt { get; set; }
}

public class PerformanceReport
{
    public DateTime GeneratedAt { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalOperations { get; set; }
    public int TotalSuccess { get; set; }
    public int TotalFailures { get; set; }
    public double AverageDuration { get; set; }
    public List<PerformanceMetric> SlowestOperations { get; set; } = new();
    public List<PerformanceMetric> MostFrequentOperations { get; set; } = new();
    public List<PerformanceMetric> OperationsBySuccessRate { get; set; } = new();
}

public class PerformanceWarning
{
    public string Operation { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium"; // Low/Medium/High
}

