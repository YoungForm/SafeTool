namespace SafeTool.Application.Services;

/// <summary>
/// 组件附件管理服务
/// </summary>
public class ComponentAttachmentService
{
    private readonly string _attachmentDir;
    private readonly string _metaPath;
    private readonly object _lock = new();
    private Dictionary<string, List<ComponentAttachment>> _attachments = new();

    public ComponentAttachmentService(string dataDir)
    {
        _attachmentDir = Path.Combine(dataDir, "ComponentAttachments");
        Directory.CreateDirectory(_attachmentDir);
        _metaPath = Path.Combine(_attachmentDir, "attachments.json");
        Load();
    }

    private void Load()
    {
        if (File.Exists(_metaPath))
        {
            var json = File.ReadAllText(_metaPath);
            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<ComponentAttachment>>>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data != null)
                _attachments = data;
        }
    }

    private void Save()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_attachments,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_metaPath, json);
    }

    /// <summary>
    /// 添加附件
    /// </summary>
    public ComponentAttachment AddAttachment(string componentId, string name, string type, Microsoft.AspNetCore.Http.IFormFile file, string? description = null)
    {
        var attachmentId = Guid.NewGuid().ToString("N");
        var fileName = $"{componentId}_{attachmentId}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(_attachmentDir, fileName);

        using (var stream = File.Create(filePath))
        {
            file.CopyTo(stream);
        }

        var attachment = new ComponentAttachment
        {
            Id = attachmentId,
            ComponentId = componentId,
            Name = name,
            Type = type,
            FileName = fileName,
            FilePath = filePath,
            ContentType = file.ContentType,
            Size = file.Length,
            Description = description,
            UploadedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            if (!_attachments.TryGetValue(componentId, out var list))
            {
                list = new List<ComponentAttachment>();
                _attachments[componentId] = list;
            }
            list.Add(attachment);
            Save();
        }

        return attachment;
    }

    /// <summary>
    /// 获取组件的所有附件
    /// </summary>
    public IEnumerable<ComponentAttachment> GetAttachments(string componentId)
    {
        lock (_lock)
        {
            return _attachments.TryGetValue(componentId, out var list) ? list : Enumerable.Empty<ComponentAttachment>();
        }
    }

    /// <summary>
    /// 获取附件文件
    /// </summary>
    public (string path, string name, string contentType)? GetAttachmentFile(string componentId, string attachmentId)
    {
        var attachments = GetAttachments(componentId);
        var attachment = attachments.FirstOrDefault(a => a.Id == attachmentId);
        
        if (attachment == null || !File.Exists(attachment.FilePath))
            return null;

        return (attachment.FilePath, attachment.Name, attachment.ContentType);
    }

    /// <summary>
    /// 删除附件
    /// </summary>
    public bool DeleteAttachment(string componentId, string attachmentId)
    {
        lock (_lock)
        {
            if (!_attachments.TryGetValue(componentId, out var list))
                return false;

            var attachment = list.FirstOrDefault(a => a.Id == attachmentId);
            if (attachment == null)
                return false;

            // 删除文件
            if (File.Exists(attachment.FilePath))
            {
                try
                {
                    File.Delete(attachment.FilePath);
                }
                catch { }
            }

            list.Remove(attachment);
            if (list.Count == 0)
                _attachments.Remove(componentId);
            
            Save();
            return true;
        }
    }
}

public class ComponentAttachment
{
    public string Id { get; set; } = string.Empty;
    public string ComponentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // datasheet/certificate/manual/other
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
}

