using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Repositories;

namespace ERP.WorkflowwServices.API.Interfaces;

public interface IPermissionService
{
    Task<PagedResult<PermissionListDto>> GetAllAsync(PermissionFilter filter, CancellationToken cancellationToken = default);
    Task<PermissionDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PermissionDto> CreateAsync(PermissionSaveRequest request, CancellationToken cancellationToken = default);
    Task<PermissionDto?> UpdateAsync(Guid id, PermissionSaveRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionDropdownDto>> GetDropdownAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<PermissionTypeDto> GetTypes();
    Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task RestoreAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> BulkArchiveAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<int> BulkRestoreAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
}
