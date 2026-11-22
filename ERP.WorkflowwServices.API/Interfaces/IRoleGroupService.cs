using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Repositories;

namespace ERP.WorkflowwServices.API.Interfaces;

public interface IRoleGroupService
{
    Task<PagedResult<RoleGroupDto>> GetAllAsync(RoleGroupFilter filter, CancellationToken cancellationToken = default);
    Task<RoleGroupDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<RoleGroupDto> SaveAsync(RoleGroupSaveRequest request, long? id = null, CancellationToken cancellationToken = default);
    Task ArchiveAsync(long id, CancellationToken cancellationToken = default);
    Task RestoreAsync(long id, CancellationToken cancellationToken = default);
    Task<int> BulkArchiveAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    Task<int> BulkRestoreAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    Task<List<RoleGroupDropdownDto>> GetDropdownAsync(Guid? companyId = null, CancellationToken cancellationToken = default);
    Task<List<RoleGroupRoleDto>?> GetRolesAsync(long id, CancellationToken cancellationToken = default);
}
