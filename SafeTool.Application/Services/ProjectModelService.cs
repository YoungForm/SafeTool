using System.Text.Json;

namespace SafeTool.Application.Services;

public class ProjectModelService
{
    private readonly string _path;
    private readonly object _lock = new();
    private Project _project = new();

    public ProjectModelService(string dataDir)
    {
        _path = Path.Combine(dataDir, "project.json");
        if (File.Exists(_path))
        {
            var json = File.ReadAllText(_path);
            var p = JsonSerializer.Deserialize<Project>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (p is not null) _project = p;
        }
        else Persist();
    }

    public IEnumerable<Function> List()
    {
        lock (_lock) return _project.Functions;
    }

    public Function Upsert(Function f)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(f.Id)) f.Id = Guid.NewGuid().ToString("N");
            _project.Functions.RemoveAll(x => x.Id == f.Id);
            _project.Functions.Add(f);
            Persist();
            return f;
        }
    }

    public string ExportJson()
    {
        lock (_lock) return JsonSerializer.Serialize(_project, new JsonSerializerOptions { WriteIndented = true });
    }

    public int ImportJson(string json)
    {
        var p = JsonSerializer.Deserialize<Project>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (p is null) return 0;
        lock (_lock)
        {
            _project = p;
            Persist();
            return _project.Functions.Count;
        }
    }

    private void Persist()
    {
        var json = JsonSerializer.Serialize(_project, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }

    public class Project
    {
        public Meta Meta { get; set; } = new();
        public List<Function> Functions { get; set; } = new();
    }

    public class Meta
    {
        public string Name { get; set; } = "Demo";
        public string Standard { get; set; } = "both";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Author { get; set; } = "unknown";
    }

    public class Function
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Standard { get; set; } = "ISO13849";
        public string Target { get; set; } = "PLc";
        public ModelSpec Model { get; set; } = new();
        public Dictionary<string, string>? Mappings { get; set; }
        public Dictionary<string, string>? Options { get; set; }
    }

    public class ModelSpec
    {
        public List<DeviceRef> I { get; set; } = new();
        public List<DeviceRef> L { get; set; } = new();
        public List<DeviceRef> O { get; set; } = new();
    }

    public class DeviceRef
    {
        public string Id { get; set; } = string.Empty;
        public Dictionary<string, string>? OverrideParams { get; set; }
    }
}

