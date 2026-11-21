using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize(Roles = "SuperAdmin")]
public sealed class DepartmentsController(IDepartmentService departments) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DepartmentFilter filter, CancellationToken cancellationToken) =>
        Ok(await departments.GetAllAsync(filter, cancellationToken));

    [HttpGet("dropdown")]
    public async Task<IActionResult> Dropdown(CancellationToken cancellationToken) => Ok(await departments.GetDropdownAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await departments.GetByIdAsync(id, cancellationToken);
        return result == null ? NotFound(new { message = "Department was not found." }) : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DepartmentSaveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await departments.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Department code or hierarchy constraint already exists." }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DepartmentSaveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await departments.UpdateAsync(id, request, cancellationToken);
            return result == null ? NotFound(new { message = "Department was not found." }) : Ok(result);
        }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Department code or hierarchy constraint already exists." }); }
    }

    [HttpPatch("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        try { await departments.ArchiveAsync(id, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    [HttpPatch("{id:guid}/unarchive")]
    public async Task<IActionResult> Unarchive(Guid id, CancellationToken cancellationToken)
    {
        try { await departments.RestoreAsync(id, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }
}
