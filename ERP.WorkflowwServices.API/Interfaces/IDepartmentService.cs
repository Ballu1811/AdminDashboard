using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Repositories;

namespace ERP.WorkflowwServices.API.Interfaces;

public interface IDepartmentService
{
    Task<PagedResult<DepartmentDto>> GetAllAsync(DepartmentFilter filter, CancellationToken cancellationToken = default);
    Task<DepartmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateAsync(DepartmentSaveRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentDto?> UpdateAsync(Guid id, DepartmentSaveRequest request, CancellationToken cancellationToken = default);
    Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task RestoreAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<DepartmentDropdownDto>> GetDropdownAsync(CancellationToken cancellationToken = default);
}
