using ERP.WorkflowwServices.API.Models;

namespace ERP.WorkflowwServices.API.Interfaces
{
    public interface IMenuService
    {
        Task<List<MenuItem>> GetMenuTreeAsync();

        Task<MenuItem?> GetByIdAsync(Guid id);

        Task<MenuItem> CreateAsync(MenuItem menu);

        Task UpdateAsync(MenuItem menu);

        Task DeleteAsync(Guid id);

        Task<bool> ExistsAsync(string label, Guid? parentId);
    }
}
