using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Controllers;

[ApiController]
[Route("api/role-permissions")]
[Authorize(Roles = "SuperAdmin")]
public sealed class RolePermissionsController(IRolePermissionMappingService rolePermissions) : ControllerBase
{
    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyList<RolePermissionRoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles([FromQuery] Guid? departmentId, CancellationToken cancellationToken)
    {
        try { return Ok(await rolePermissions.GetRolesAsync(departmentId, cancellationToken)); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
    }

    [HttpGet("permissions/tree")]
    [ProducesResponseType(typeof(IReadOnlyList<RolePermissionItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionTree(CancellationToken cancellationToken) =>
        Ok(await rolePermissions.GetPermissionTreeAsync(cancellationToken));

    [HttpGet("role/{roleId:guid}")]
    [ProducesResponseType(typeof(RolePermissionMappingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoleMapping(Guid roleId, CancellationToken cancellationToken)
    {
        var mapping = await rolePermissions.GetRoleMappingAsync(roleId, cancellationToken);
        return mapping is null ? NotFound(new { message = "The selected active role was not found." }) : Ok(mapping);
    }

    [HttpPut("role/{roleId:guid}")]
    [ProducesResponseType(typeof(RolePermissionMappingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveRoleMapping(
        Guid roleId,
        [FromBody] RolePermissionSaveRequest request,
        CancellationToken cancellationToken)
    {
        try { return Ok(await rolePermissions.SaveRoleMappingAsync(roleId, request, cancellationToken)); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "The role permission mapping could not be saved due to a conflicting mapping." }); }
    }
}
