using ERP.WorkflowwServices.API.DTOs;

namespace ERP.WorkflowwServices.API.Interfaces;

public interface IRolePermissionMappingService
{
    Task<IReadOnlyList<RolePermissionRoleDto>> GetRolesAsync(Guid? departmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RolePermissionItemDto>> GetPermissionTreeAsync(CancellationToken cancellationToken = default);
    Task<RolePermissionMappingDto?> GetRoleMappingAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<RolePermissionMappingDto> SaveRoleMappingAsync(Guid roleId, RolePermissionSaveRequest request, CancellationToken cancellationToken = default);
}
