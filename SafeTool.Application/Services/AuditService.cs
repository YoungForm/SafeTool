namespace SafeTool.Application.Services;

public class AuditRecord
{
    public DateTime Time { get; set; } = DateTime.UtcNow;
    public string User { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public class AuditService
{
    private readonly List<AuditRecord> _records = new();
    private readonly string? _filePath;
    public AuditService() { }
    public AuditService(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "audit.log.jsonl");
        if (File.Exists(_filePath))
        {
            foreach (var line in File.ReadLines(_filePath))
            {
                try
                {
                    var r = System.Text.Json.JsonSerializer.Deserialize<AuditRecord>(line);
                    if (r != null) _records.Add(r);
                }
                catch { }
            }
        }
    }
    public void Log(string user, string action, string resource, string detail)
    {
        var rec = new AuditRecord { User = user, Action = action, Resource = resource, Detail = detail };
        _records.Add(rec);
        if (!string.IsNullOrEmpty(_filePath))
            File.AppendAllText(_filePath, System.Text.Json.JsonSerializer.Serialize(rec) + Environment.NewLine);
        if (_records.Count > 5000) _records.RemoveRange(0, _records.Count - 4000);
    }
    public IEnumerable<AuditRecord> Query(string? user = null, string? action = null, int skip = 0, int take = 200)
    {
        IEnumerable<AuditRecord> q = _records;
        if (!string.IsNullOrWhiteSpace(user)) q = q.Where(r => r.User.Equals(user, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(r => r.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
        return q.OrderByDescending(r => r.Time).Skip(skip).Take(take);
    }
}
