using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Repositories;

namespace ERP.WorkflowwServices.API.Interfaces
{
    public interface IUserManagementService
    {
        Task<PagedResult<UserListDto>> GetAllAsync(FilterModel filter);
        Task<UserListDto?> GetByIdAsync(Guid id);
        Task<List<UserRoleOptionDto>> GetRolesAsync();
        Task<UserListDto> CreateAsync(CreateUserRequest request);
        Task<UserListDto?> UpdateAsync(Guid id, UpdateUserRequest request);
        Task<UserListDto?> SetRolesAsync(Guid id, SetUserRolesRequest request);
        Task<UserListDto?> SetStatusAsync(Guid id, bool isActive);
    }
}