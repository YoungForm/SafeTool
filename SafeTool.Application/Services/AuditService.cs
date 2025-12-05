using System.Text.Json;

namespace SafeTool.Application.Services;

public class AuditRecord
{
    public DateTime Time { get; set; } = DateTime.UtcNow;
    public string User { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string? Signature { get; set; } // 审计事件签名
}

public class AuditService
{
    private readonly List<AuditRecord> _records = new();
    private readonly string? _filePath;
    private readonly object _lock = new();
    private readonly IServiceProvider? _serviceProvider;

    public AuditService() { }

    public AuditService(string dataDir, IServiceProvider? serviceProvider = null)
    {
        _serviceProvider = serviceProvider;
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "audit.log.jsonl");
        if (File.Exists(_filePath))
        {
            foreach (var line in File.ReadLines(_filePath))
            {
                try
                {
                    var r = JsonSerializer.Deserialize<AuditRecord>(line);
                    if (r != null) _records.Add(r);
                }
                catch { }
            }
        }
    }

    public void Log(string user, string action, string resource, string detail)
    {
        var entry = new AuditRecord
        {
            Time = DateTime.UtcNow,
            User = user,
            Action = action,
            Resource = resource,
            Detail = detail
        };
        
        // 如果启用了签名服务，对审计事件进行签名
        var signatureService = _serviceProvider?.GetService(typeof(AuditSignatureService)) as AuditSignatureService;
        if (signatureService != null)
        {
            var auditEvent = new AuditEvent
            {
                Timestamp = entry.Time,
                User = user,
                Action = action,
                Resource = resource,
                Detail = detail
            };
            entry.Signature = signatureService.SignAuditEvent(auditEvent);
        }
        
        lock (_lock)
        {
            _records.Add(entry);
            if (!string.IsNullOrEmpty(_filePath))
                File.AppendAllText(_filePath, JsonSerializer.Serialize(entry) + Environment.NewLine);
            if (_records.Count > 5000) _records.RemoveRange(0, _records.Count - 4000);
        }
    }

    public IEnumerable<AuditRecord> Query(string? user = null, string? action = null, int skip = 0, int take = 200)
    {
        lock (_lock)
        {
            IEnumerable<AuditRecord> q = _records;
            if (!string.IsNullOrWhiteSpace(user)) q = q.Where(r => r.User.Equals(user, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(action)) q = q.Where(r => r.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
            return q.OrderByDescending(r => r.Time).Skip(skip).Take(take);
        }
    }

    /// <summary>
    /// 验证审计记录签名
    /// </summary>
    public bool VerifySignature(AuditRecord record, AuditSignatureService signatureService)
    {
        if (string.IsNullOrWhiteSpace(record.Signature))
            return false;

        var auditEvent = new AuditEvent
        {
            Timestamp = record.Time,
            User = record.User,
            Action = record.Action,
            Resource = record.Resource,
            Detail = record.Detail
        };

        return signatureService.VerifyAuditEvent(auditEvent, record.Signature);
    }
}
