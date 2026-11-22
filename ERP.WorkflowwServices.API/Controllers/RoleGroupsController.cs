using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Controllers;

[ApiController]
[Route("api/role-groups")]
[Authorize(Roles = "SuperAdmin")]
public sealed class RoleGroupsController(IRoleGroupService roleGroups) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RoleGroupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] RoleGroupFilter filter, CancellationToken cancellationToken) =>
        Ok(await roleGroups.GetAllAsync(filter, cancellationToken));

    [HttpGet("dropdown")]
    public async Task<IActionResult> GetDropdown([FromQuery] Guid? companyId, CancellationToken cancellationToken) =>
        Ok(await roleGroups.GetDropdownAsync(companyId, cancellationToken));

    [HttpGet("{id:long}/roles")]
    public async Task<IActionResult> GetRoles(long id, CancellationToken cancellationToken)
    {
        var roles = await roleGroups.GetRolesAsync(id, cancellationToken);
        return roles is null ? NotFound(new { message = "Role group was not found." }) : Ok(roles);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var group = await roleGroups.GetByIdAsync(id, cancellationToken);
        return group is null ? NotFound(new { message = "Role group was not found." }) : Ok(group);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RoleGroupSaveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var group = await roleGroups.SaveAsync(request, cancellationToken: cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
        }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Role group name, code, or role mapping already exists." }); }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] RoleGroupSaveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var group = await roleGroups.SaveAsync(request, id, cancellationToken);
            return Ok(group);
        }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Role group name, code, or role mapping already exists." }); }
    }

    [HttpPatch("{id:long}/archive")]
    public async Task<IActionResult> Archive(long id, CancellationToken cancellationToken)
    {
        try { await roleGroups.ArchiveAsync(id, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
    }

    [HttpPatch("{id:long}/restore")]
    public async Task<IActionResult> Restore(long id, CancellationToken cancellationToken)
    {
        try { await roleGroups.RestoreAsync(id, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Role group name or code already exists." }); }
    }

    [HttpPost("bulk-archive")]
    public async Task<IActionResult> BulkArchive([FromBody] List<long> ids, CancellationToken cancellationToken)
    {
        try { return Ok(new { archived = await roleGroups.BulkArchiveAsync(ids ?? [], cancellationToken) }); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    [HttpPost("bulk-restore")]
    public async Task<IActionResult> BulkRestore([FromBody] List<long> ids, CancellationToken cancellationToken)
    {
        try { return Ok(new { restored = await roleGroups.BulkRestoreAsync(ids ?? [], cancellationToken) }); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Role group name or code already exists." }); }
    }
}
