using System.Text.Json;
using SafeTool.Domain.ChangeManagement;

namespace SafeTool.Application.Repositories;

/// <summary>
/// 基于文件的变更请求仓储实现（Repository模式）
/// </summary>
public class FileBasedChangeRequestRepository : IChangeRequestRepository
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private Dictionary<string, ChangeRequest> _store = new();

    public FileBasedChangeRequestRepository(string dataDir)
    {
        var dir = Path.Combine(dataDir, "ChangeManagement");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "changerequests.json");
        Load();
    }

    private void Load()
    {
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, ChangeRequest>>(
                json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data != null)
                _store = data;
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_store, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    public Task<ChangeRequest?> GetByIdAsync(string id)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.TryGetValue(id, out var cr) ? cr : null);
        }
    }

    public Task<IEnumerable<ChangeRequest>> GetByProjectIdAsync(string projectId)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.Values.Where(cr => cr.ProjectId == projectId));
        }
    }

    public Task<IEnumerable<ChangeRequest>> GetByStatusAsync(ChangeStatus status)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.Values.Where(cr => cr.Status == status));
        }
    }

    public Task<IEnumerable<ChangeRequest>> GetByRequesterAsync(string requester)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.Values.Where(cr => cr.Requester == requester));
        }
    }

    public Task<ChangeRequest> CreateAsync(ChangeRequest changeRequest)
    {
        lock (_lock)
        {
            _store[changeRequest.Id] = changeRequest;
            Save();
            return Task.FromResult(changeRequest);
        }
    }

    public Task<ChangeRequest> UpdateAsync(ChangeRequest changeRequest)
    {
        lock (_lock)
        {
            if (!_store.ContainsKey(changeRequest.Id))
                throw new KeyNotFoundException($"变更请求 {changeRequest.Id} 不存在");
            
            _store[changeRequest.Id] = changeRequest;
            Save();
            return Task.FromResult(changeRequest);
        }
    }

    public Task<bool> DeleteAsync(string id)
    {
        lock (_lock)
        {
            var removed = _store.Remove(id);
            if (removed)
                Save();
            return Task.FromResult(removed);
        }
    }
}

