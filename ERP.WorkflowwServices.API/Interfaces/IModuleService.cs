using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;

namespace ERP.WorkflowwServices.API.Interfaces
{
    public interface IModuleService
    {
        Task<PagedResult<ModuleDto>> GetAllAsync(FilterModel filter);
        Task<ModuleDto?> GetByIdAsync(Guid id);
        Task<ModuleDto> CreateAsync(ModuleCreateDto dto);
        Task UpdateAsync(ModuleUpdateDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(string name, string code);
    }
}
