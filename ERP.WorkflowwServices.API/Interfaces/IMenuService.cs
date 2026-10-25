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
        Task<List<MenuDto>> GetMenuTreeByModuleNameAsync(string moduleName);
        Task<List<SidebarModuleDto>> GetSidebarModulesAsync();
        Task<PagedResult<MenuDto>> GetAllAsync(FilterModel filter);
        Task<MenuDto?> GetByIdAsync(Guid id);
        Task<MenuDto> CreateAsync(MenuCreateDto menu);
        Task UpdateAsync(MenuUpdateDto model);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(string label, Guid? moduleId, Guid? parentId, Guid? excludeId = null);
        Task<List<MenuPlacementDto>> GetPlacementsAsync();
        Task<MenuPlacementDto?> GetPlacementByIdAsync(Guid id);
        Task<MenuPlacementDto> CreatePlacementAsync(MenuPlacementCreateDto model);
        Task UpdatePlacementAsync(MenuPlacementUpdateDto model);
        Task DeletePlacementAsync(Guid id);
    }
}
