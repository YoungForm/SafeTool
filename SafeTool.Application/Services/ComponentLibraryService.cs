using System.Text.Json;

namespace SafeTool.Application.Services;

public class ComponentLibraryService
{
    private readonly string _path;
    private readonly object _lock = new();
    private Library _cache = new() { Version = "1.0.0" };

    public ComponentLibraryService(string dataDir)
    {
        _path = Path.Combine(dataDir, "components.json");
        if (File.Exists(_path))
        {
            var json = File.ReadAllText(_path);
            var lib = JsonSerializer.Deserialize<Library>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (lib is not null) _cache = lib;
        }
        else
        {
            Persist();
        }
    }

    public IEnumerable<ComponentRecord> List() => _cache.Items;

    public ComponentRecord? Get(string id) => _cache.Items.FirstOrDefault(i => i.Id == id);

    public ComponentRecord Add(ComponentRecord item)
    {
        lock (_lock)
        {
            _cache.Items.RemoveAll(i => i.Id == item.Id);
            _cache.Items.Add(item);
            Persist();
        }
        return item;
    }

    public bool Update(string id, ComponentRecord item)
    {
        lock (_lock)
        {
            var idx = _cache.Items.FindIndex(i => i.Id == id);
            if (idx < 0) return false;
            item.Id = id;
            _cache.Items[idx] = item;
            Persist();
            return true;
        }
    }

    public bool Delete(string id)
    {
        lock (_lock)
        {
            var removed = _cache.Items.RemoveAll(i => i.Id == id) > 0;
            if (removed) Persist();
            return removed;
        }
    }

    public int ImportJson(string json)
    {
        var lib = JsonSerializer.Deserialize<Library>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (lib is null) return 0;
        lock (_lock)
        {
            _cache = lib;
            Persist();
        }
        return _cache.Items.Count;
    }

    public string ExportJson() => JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });

    private void Persist()
    {
        _cache.UpdatedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }

    public class Library
    {
        public string Version { get; set; } = "1.0.0";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<ComponentRecord> Items { get; set; } = new();
    }

    public class ComponentRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new();
        public Dictionary<string, string>? Environment { get; set; }
    }
}

