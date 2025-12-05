using SafeTool.Domain.ChangeManagement;

namespace SafeTool.Application.Repositories;

/// <summary>
/// 变更请求仓储接口（Repository模式）
/// </summary>
public interface IChangeRequestRepository
{
    Task<ChangeRequest?> GetByIdAsync(string id);
    Task<IEnumerable<ChangeRequest>> GetByProjectIdAsync(string projectId);
    Task<IEnumerable<ChangeRequest>> GetByStatusAsync(ChangeStatus status);
    Task<IEnumerable<ChangeRequest>> GetByRequesterAsync(string requester);
    Task<ChangeRequest> CreateAsync(ChangeRequest changeRequest);
    Task<ChangeRequest> UpdateAsync(ChangeRequest changeRequest);
    Task<bool> DeleteAsync(string id);
}

