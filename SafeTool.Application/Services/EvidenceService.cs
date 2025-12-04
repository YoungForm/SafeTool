using System.Text.Json;

namespace SafeTool.Application.Services;

public class EvidenceService
{
    private readonly string _dir;
    private readonly string _metaPath;
    private readonly object _lock = new();
    private List<Evidence> _items = new();
    private Dictionary<string, List<Link>> _links = new();

    public EvidenceService(string dataDir)
    {
        _dir = Path.Combine(dataDir, "Evidence");
        Directory.CreateDirectory(_dir);
        _metaPath = Path.Combine(_dir, "evidence.json");
        if (File.Exists(_metaPath))
        {
            var json = File.ReadAllText(_metaPath);
            var data = JsonSerializer.Deserialize<Data>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data is not null)
            {
                _items = data.Items ?? new();
                _links = data.Links ?? new();
            }
        }
        else Persist();
    }

    public IEnumerable<Evidence> List(string? type, string? status)
    {
        return _items.Where(x => (string.IsNullOrWhiteSpace(type) || x.Type == type) && (string.IsNullOrWhiteSpace(status) || x.Status == status));
    }

    public Evidence? Get(string id)
    {
        return _items.FirstOrDefault(x => x.Id == id);
    }

    public (string path, string name, string contentType)? GetFile(string id)
    {
        var e = Get(id);
        if (e is null || string.IsNullOrWhiteSpace(e.FilePath) || !File.Exists(e.FilePath)) return null;
        var ext = Path.GetExtension(e.FilePath).ToLowerInvariant();
        var ct = ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".txt" => "text/plain",
            ".html" => "text/html",
            _ => "application/octet-stream"
        };
        var name = Path.GetFileName(e.FilePath);
        return (e.FilePath, name, ct);
    }

    public Evidence Add(string name, string type, string? note, Microsoft.AspNetCore.Http.IFormFile? file, string? source = null, string? issuer = null, DateTime? validUntil = null, string? url = null)
    {
        var id = Guid.NewGuid().ToString("N");
        string? saved = null;
        if (file is not null && file.Length > 0)
        {
            var path = Path.Combine(_dir, id + "_" + Path.GetFileName(file.FileName));
            using var s = File.Create(path);
            file.CopyTo(s);
            saved = path;
        }
        var e = new Evidence { Id = id, Name = name, Type = type, Note = note, FilePath = saved, Source = source, Issuer = issuer, ValidUntil = validUntil, Url = url, Status = "new", CreatedAt = DateTime.UtcNow };
        lock (_lock)
        {
            _items.Add(e);
            Persist();
        }
        return e;
    }

    public Link CreateLink(string evidenceId, string resourceType, string resourceId)
    {
        var l = new Link { EvidenceId = evidenceId, ResourceType = resourceType, ResourceId = resourceId };
        lock (_lock)
        {
            if (!_links.TryGetValue(evidenceId, out var list)) { list = new List<Link>(); _links[evidenceId] = list; }
            list.Add(l);
            Persist();
        }
        return l;
    }

    private void Persist()
    {
        var json = JsonSerializer.Serialize(new Data { Items = _items, Links = _links }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_metaPath, json);
    }

    public class Evidence
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // certificate/report/photo
        public string? Note { get; set; }
        public string? FilePath { get; set; }
        public string? Source { get; set; }
        public string? Issuer { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string? Url { get; set; }
        public string Status { get; set; } = "new";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Link
    {
        public string EvidenceId { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty; // function/calculation/checklist
        public string ResourceId { get; set; } = string.Empty;
    }

    private class Data
    {
        public List<Evidence>? Items { get; set; }
        public Dictionary<string, List<Link>>? Links { get; set; }
    }
}
