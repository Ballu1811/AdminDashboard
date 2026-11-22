using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Repositories;

namespace ERP.WorkflowwServices.API.Interfaces;

public interface IUserRoleAssignmentService
{
    Task<PagedResult<UserRoleAssignmentDto>> GetAllAsync(UserRoleAssignmentFilter filter);
    Task<UserRoleAssignmentSummaryDto> GetSummaryAsync();
    Task<IReadOnlyList<UserRoleAssignmentUserOptionDto>> GetUsersAsync(string? search, int pageNumber, int pageSize);
    Task<IReadOnlyList<UserRoleAssignmentRoleOptionDto>> GetRolesAsync();
    Task<IReadOnlyList<UserRoleAssignmentDepartmentOptionDto>> GetDepartmentsAsync();
    Task<IReadOnlyList<UserRoleAssignmentDto>> GetByUserAsync(Guid userId);
    Task<UserRoleAssignmentDto?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<UserRoleAssignmentHistoryDto>> GetHistoryAsync(Guid userId);
    Task<IReadOnlyList<UserRoleAssignmentDto>> SaveForUserAsync(Guid userId, IReadOnlyCollection<UserRoleAssignmentInput> assignments);
    Task DeactivateAsync(Guid id);
    Task<int> BulkDeactivateAsync(IReadOnlyCollection<Guid> ids);
    Task SyncLegacyRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, bool allowInactiveUser = false);
}
