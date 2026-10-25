using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Repositories;

namespace ERP.WorkflowwServices.API.Interfaces;

public interface ISubModuleService
{
    Task<PagedResult<SubModuleDto>> GetAllAsync(FilterModel filter);
    Task<SubModuleDto?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<SubModuleDto>> GetByParentIdAsync(Guid parentId);
    Task<SubModuleDto> CreateAsync(SubModuleCreateDto dto);
    Task UpdateAsync(SubModuleUpdateDto dto);
    Task ArchiveAsync(Guid id);
    Task RestoreAsync(Guid id);
}
