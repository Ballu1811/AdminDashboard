using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Repositories;

namespace ERP.WorkflowwServices.API.Interfaces;

public interface IPositionService
{
    Task<PagedResult<PositionDto>> GetAllAsync(PositionFilter filter, CancellationToken cancellationToken = default);
    Task<PositionDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<PositionDto> SaveAsync(PositionSaveRequest request, long? id = null, CancellationToken cancellationToken = default);
    Task<bool> IsCodeAvailableAsync(Guid? companyId, string code, long? excludeId = null, CancellationToken cancellationToken = default);
    Task ArchiveAsync(long id, CancellationToken cancellationToken = default);
    Task RestoreAsync(long id, CancellationToken cancellationToken = default);
    Task<int> BulkArchiveAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    Task<int> BulkRestoreAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    Task<List<PositionHierarchyDto>> GetHierarchyAsync(Guid? companyId = null, CancellationToken cancellationToken = default);
    Task<List<PositionDropdownDto>> GetDropdownAsync(Guid? companyId = null, CancellationToken cancellationToken = default);
    Task<List<PositionRoleDto>?> GetRolesAsync(long id, CancellationToken cancellationToken = default);
}
