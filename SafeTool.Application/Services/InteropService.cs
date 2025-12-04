using System.Text.Json;

namespace SafeTool.Application.Services;

public class InteropService
{
    public string ExportJson(SafeTool.Domain.Interop.ProjectDto project)
    {
        return JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });
    }

    public object ExportTarget(SafeTool.Domain.Interop.ProjectDto project, string target)
    {
        target = target.ToLowerInvariant();
        if (target is "json") return project;
        var sum = project.Functions.Select(f => new
        {
            id = f.Id,
            name = f.Name,
            subsystems = f.Subsystems.Count,
            components = f.Subsystems.Sum(s => s.Components.Count)
        }).ToList();
        return new { target, summary = sum };
    }

    public SafeTool.Domain.Interop.ProjectDto ImportJson(string json)
    {
        var dto = JsonSerializer.Deserialize<SafeTool.Domain.Interop.ProjectDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                  ?? new SafeTool.Domain.Interop.ProjectDto();
        return dto;
    }
}

