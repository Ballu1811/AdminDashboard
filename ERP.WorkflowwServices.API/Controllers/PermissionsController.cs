using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Roles = "SuperAdmin")]
public sealed class PermissionsController(IPermissionService permissions) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PermissionListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PermissionFilter filter, CancellationToken cancellationToken) =>
        Ok(await permissions.GetAllAsync(filter, cancellationToken));

    [HttpGet("dropdown")]
    public async Task<IActionResult> GetDropdown(CancellationToken cancellationToken) =>
        Ok(await permissions.GetDropdownAsync(cancellationToken));

    [HttpGet("types")]
    public IActionResult GetTypes() => Ok(permissions.GetTypes());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var permission = await permissions.GetByIdAsync(id, cancellationToken);
        return permission is null ? NotFound(new { message = "Permission was not found." }) : Ok(permission);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PermissionSaveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var permission = await permissions.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = permission.Id }, permission);
        }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Permission key or mapping already exists." }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PermissionSaveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var permission = await permissions.UpdateAsync(id, request, cancellationToken);
            return permission is null ? NotFound(new { message = "Permission was not found." }) : Ok(permission);
        }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Permission key or mapping already exists." }); }
    }

    [HttpPatch("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        try { await permissions.ArchiveAsync(id, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    [HttpPatch("{id:guid}/unarchive")]
    public async Task<IActionResult> Unarchive(Guid id, CancellationToken cancellationToken)
    {
        try { await permissions.RestoreAsync(id, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Permission key or mapping already exists." }); }
    }

    [HttpPost("bulk-archive")]
    public async Task<IActionResult> BulkArchive([FromBody] List<Guid> ids, CancellationToken cancellationToken)
    {
        try { return Ok(new { archived = await permissions.BulkArchiveAsync(ids ?? [], cancellationToken) }); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    [HttpPost("bulk-restore")]
    public async Task<IActionResult> BulkRestore([FromBody] List<Guid> ids, CancellationToken cancellationToken)
    {
        try { return Ok(new { restored = await permissions.BulkRestoreAsync(ids ?? [], cancellationToken) }); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Permission key or mapping already exists." }); }
    }
}
