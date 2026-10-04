using ERP.WorkflowwServices.API.DTOs;

namespace ERP.WorkflowwServices.API.Interfaces
{
    public interface IRoleManagementService
    {
        Task<List<RoleListDto>> GetAllAsync();
        Task<RoleListDto> CreateAsync(SaveRoleRequest request);
        Task<RoleListDto?> UpdateAsync(Guid id, SaveRoleRequest request);
    }
}
