using System.Text.Json;

namespace SafeTool.Application.Services;

public class ComplianceMatrixService
{
    private readonly string _path;
    private readonly object _lock = new();
    private Data _data = new();

    public ComplianceMatrixService(string dataDir)
    {
        _path = Path.Combine(dataDir, "compliance-matrix.json");
        if (File.Exists(_path))
        {
            var json = File.ReadAllText(_path);
            var d = JsonSerializer.Deserialize<Data>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (d is not null) _data = d;
        }
        else Persist();
    }

    public IEnumerable<Entry> Get(string projectId)
    {
        lock (_lock)
        {
            return _data.Items.TryGetValue(projectId, out var list) ? list : Enumerable.Empty<Entry>();
        }
    }

    public Entry Add(string projectId, Entry e)
    {
        lock (_lock)
        {
            if (!_data.Items.TryGetValue(projectId, out var list))
            {
                list = new List<Entry>();
                _data.Items[projectId] = list;
            }
            e.Id = e.Id ?? Guid.NewGuid().ToString("N");
            list.RemoveAll(x => x.Id == e.Id);
            list.Add(e);
            Persist();
            return e;
        }
    }

    public string ExportCsv(string projectId)
    {
        var list = Get(projectId).ToList();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("标准,条款,要求摘要,引用,证据ID,结果,责任人,期限");
        foreach (var x in list)
            sb.AppendLine($"{x.Standard},{x.Clause},{x.Requirement},{x.Reference},{x.EvidenceId},{x.Result},{x.Owner},{x.Due}");
        return sb.ToString();
    }

    public int ImportCsv(string projectId, string csv)
    {
        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int count = 0;
        foreach (var line in lines.Skip(1))
        {
            var parts = line.Split(',');
            if (parts.Length < 8) continue;
            var e = new Entry
            {
                Standard = parts[0].Trim(),
                Clause = parts[1].Trim(),
                Requirement = parts[2].Trim(),
                Reference = parts[3].Trim(),
                EvidenceId = string.IsNullOrWhiteSpace(parts[4]) ? null : parts[4].Trim(),
                Result = parts[5].Trim(),
                Owner = string.IsNullOrWhiteSpace(parts[6]) ? null : parts[6].Trim(),
                Due = string.IsNullOrWhiteSpace(parts[7]) ? null : parts[7].Trim()
            };
            Add(projectId, e);
            count++;
        }
        return count;
    }

    private void Persist()
    {
        var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }

    public class Data
    {
        public Dictionary<string, List<Entry>> Items { get; set; } = new();
    }

    public class Entry
    {
        public string? Id { get; set; }
        public string Standard { get; set; } = string.Empty;
        public string Clause { get; set; } = string.Empty;
        public string Requirement { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string? EvidenceId { get; set; }
        public string Result { get; set; } = string.Empty; // 符合/不符合/需整改
        public string? Owner { get; set; }
        public string? Due { get; set; }
    }
}
