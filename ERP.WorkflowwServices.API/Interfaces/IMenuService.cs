using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;

namespace ERP.WorkflowwServices.API.Interfaces
{
    public interface IMenuService
    {
        Task<List<MenuDto>> GetMenuTreeAsync();
        Task<List<MenuDto>> GetMenuTreeByModuleAsync(Guid moduleId);
        Task<PagedResult<MenuDto>> GetAllAsync(FilterModel filter);
        Task<MenuDto?> GetByIdAsync(Guid id);
        Task<MenuDto> CreateAsync(MenuCreateDto menu);
        Task UpdateAsync(MenuUpdateDto model);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(string label, Guid? parentId);
    }
}
