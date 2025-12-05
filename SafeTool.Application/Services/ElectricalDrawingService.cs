using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 电气图纸关联服务（P2优先级）
/// 管理电气图纸与安全功能的关联
/// </summary>
public class ElectricalDrawingService
{
    private readonly string _dataDir;

    public ElectricalDrawingService(string dataDir)
    {
        _dataDir = dataDir;
        EnsureDirectories();
    }

    /// <summary>
    /// 关联电气图纸
    /// </summary>
    public ElectricalDrawingLink LinkDrawing(
        string projectId,
        string resourceType,
        string resourceId,
        ElectricalDrawingInfo drawing)
    {
        var link = new ElectricalDrawingLink
        {
            Id = Guid.NewGuid().ToString(),
            ProjectId = projectId,
            ResourceType = resourceType, // SRS/Function/Component/Checklist
            ResourceId = resourceId,
            Drawing = drawing,
            LinkedAt = DateTime.UtcNow,
            LinkedBy = "system" // 应该从上下文获取用户
        };

        SaveLink(link);
        return link;
    }

    /// <summary>
    /// 获取资源关联的图纸
    /// </summary>
    public List<ElectricalDrawingLink> GetDrawings(
        string projectId,
        string? resourceType = null,
        string? resourceId = null)
    {
        var allLinks = LoadAllLinks(projectId);

        if (!string.IsNullOrEmpty(resourceType))
            allLinks = allLinks.Where(l => l.ResourceType == resourceType).ToList();

        if (!string.IsNullOrEmpty(resourceId))
            allLinks = allLinks.Where(l => l.ResourceId == resourceId).ToList();

        return allLinks;
    }

    /// <summary>
    /// 获取图纸关联的资源
    /// </summary>
    public List<ElectricalDrawingLink> GetLinkedResources(
        string projectId,
        string drawingId)
    {
        var allLinks = LoadAllLinks(projectId);
        return allLinks.Where(l => l.Drawing.Id == drawingId).ToList();
    }

    /// <summary>
    /// 删除图纸关联
    /// </summary>
    public bool UnlinkDrawing(string projectId, string linkId)
    {
        var allLinks = LoadAllLinks(projectId);
        var link = allLinks.FirstOrDefault(l => l.Id == linkId);

        if (link != null)
        {
            allLinks.Remove(link);
            SaveAllLinks(projectId, allLinks);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 验证图纸信息
    /// </summary>
    public DrawingValidationResult ValidateDrawing(ElectricalDrawingInfo drawing)
    {
        var result = new DrawingValidationResult
        {
            DrawingId = drawing.Id,
            IsValid = true,
            Issues = new List<string>()
        };

        if (string.IsNullOrEmpty(drawing.FileName))
        {
            result.IsValid = false;
            result.Issues.Add("图纸文件名不能为空");
        }

        if (string.IsNullOrEmpty(drawing.Version))
        {
            result.IsValid = false;
            result.Issues.Add("图纸版本不能为空");
        }

        if (drawing.FileSize <= 0)
        {
            result.IsValid = false;
            result.Issues.Add("图纸文件大小无效");
        }

        // 验证文件扩展名
        var validExtensions = new[] { ".dwg", ".dxf", ".pdf", ".png", ".jpg", ".jpeg" };
        var extension = Path.GetExtension(drawing.FileName).ToLowerInvariant();
        if (!validExtensions.Contains(extension))
        {
            result.IsValid = false;
            result.Issues.Add($"不支持的文件格式: {extension}");
        }

        if (result.IsValid)
        {
            result.Message = "图纸信息有效";
        }

        return result;
    }

    /// <summary>
    /// 保存关联
    /// </summary>
    private void SaveLink(ElectricalDrawingLink link)
    {
        var allLinks = LoadAllLinks(link.ProjectId);
        allLinks.RemoveAll(l => l.Id == link.Id);
        allLinks.Add(link);
        SaveAllLinks(link.ProjectId, allLinks);
    }

    /// <summary>
    /// 加载所有关联
    /// </summary>
    private List<ElectricalDrawingLink> LoadAllLinks(string projectId)
    {
        var path = GetLinksPath(projectId);
        if (!File.Exists(path))
            return new List<ElectricalDrawingLink>();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<ElectricalDrawingLink>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ElectricalDrawingLink>();
        }
        catch
        {
            return new List<ElectricalDrawingLink>();
        }
    }

    /// <summary>
    /// 保存所有关联
    /// </summary>
    private void SaveAllLinks(string projectId, List<ElectricalDrawingLink> links)
    {
        var path = GetLinksPath(projectId);
        var json = JsonSerializer.Serialize(links, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 获取关联文件路径
    /// </summary>
    private string GetLinksPath(string projectId)
    {
        var dir = Path.Combine(_dataDir, "electrical-drawings", projectId);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "links.json");
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private void EnsureDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_dataDir, "electrical-drawings"));
    }
}

public class ElectricalDrawingLink
{
    public string Id { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty; // SRS/Function/Component/Checklist
    public string ResourceId { get; set; } = string.Empty;
    public ElectricalDrawingInfo Drawing { get; set; } = new();
    public DateTime LinkedAt { get; set; }
    public string LinkedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ElectricalDrawingInfo
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? FilePath { get; set; }
    public string? DrawingNumber { get; set; }
    public string? SheetNumber { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DrawingValidationResult
{
    public string DrawingId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public List<string> Issues { get; set; } = new();
}

