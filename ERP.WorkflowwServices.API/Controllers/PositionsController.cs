using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Controllers;

[ApiController]
[Route("api/positions")]
[Authorize(Roles = "SuperAdmin")]
public sealed class PositionsController(IPositionService positions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PositionFilter filter, CancellationToken cancellationToken) =>
        Ok(await positions.GetAllAsync(filter, cancellationToken));

    [HttpGet("hierarchy")]
    public async Task<IActionResult> GetHierarchy([FromQuery] Guid? companyId, CancellationToken cancellationToken) =>
        Ok(await positions.GetHierarchyAsync(companyId, cancellationToken));

    [HttpGet("dropdown")]
    public async Task<IActionResult> GetDropdown([FromQuery] Guid? companyId, CancellationToken cancellationToken) =>
        Ok(await positions.GetDropdownAsync(companyId, cancellationToken));

    [HttpGet("code-availability")]
    [ProducesResponseType(typeof(PositionCodeAvailabilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IsCodeAvailable(
        [FromQuery] Guid? companyId,
        [FromQuery, Required, StringLength(50), RegularExpression("^[A-Za-z0-9_-]+$")] string code,
        [FromQuery] long? excludeId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(new PositionCodeAvailabilityDto(
                await positions.IsCodeAvailableAsync(companyId, code, excludeId, cancellationToken)));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var item = await positions.GetByIdAsync(id, cancellationToken);
        return item == null ? NotFound(new { message = "Position was not found." }) : Ok(item);
    }

    [HttpGet("{id:long}/roles")]
    public async Task<IActionResult> GetRoles(long id, CancellationToken cancellationToken)
    {
        var roles = await positions.GetRolesAsync(id, cancellationToken);
        return roles == null ? NotFound(new { message = "Position was not found." }) : Ok(roles);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PositionSaveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await positions.SaveAsync(request, cancellationToken: cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Position code or mapping already exists." }); }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] PositionSaveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await positions.SaveAsync(request, id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Position code or mapping already exists." }); }
    }

    [HttpPatch("{id:long}/archive")]
    public async Task<IActionResult> Archive(long id, CancellationToken cancellationToken)
    {
        try { await positions.ArchiveAsync(id, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    [HttpPatch("{id:long}/unarchive")]
    public async Task<IActionResult> Unarchive(long id, CancellationToken cancellationToken)
    {
        try { await positions.RestoreAsync(id, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    [HttpPost("bulk-archive")]
    public async Task<IActionResult> BulkArchive([FromBody] List<long> ids, CancellationToken cancellationToken)
    {
        try { return Ok(new { archived = await positions.BulkArchiveAsync(ids ?? [], cancellationToken) }); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    [HttpPost("bulk-restore")]
    public async Task<IActionResult> BulkRestore([FromBody] List<long> ids, CancellationToken cancellationToken)
    {
        try { return Ok(new { restored = await positions.BulkRestoreAsync(ids ?? [], cancellationToken) }); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Position code or hierarchy constraint already exists." }); }
    }
}
