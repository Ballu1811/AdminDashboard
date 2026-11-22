using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.WorkflowwServices.API.Controllers;

[ApiController]
[Route("api/user-role-assignments")]
[Authorize(Roles = "SuperAdmin")]
public sealed class UserRoleAssignmentsController(IUserRoleAssignmentService assignments) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] UserRoleAssignmentFilter filter)
        => Ok(await assignments.GetAllAsync(filter));

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
        => Ok(await assignments.GetSummaryAsync());

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        => Ok(await assignments.GetUsersAsync(search, pageNumber, pageSize));

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
        => Ok(await assignments.GetRolesAsync());

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
        => Ok(await assignments.GetDepartmentsAsync());

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        try { return Ok(await assignments.GetByUserAsync(userId)); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
    }

    [HttpGet("user/{userId:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid userId)
    {
        try { return Ok(await assignments.GetHistoryAsync(userId)); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var assignment = await assignments.GetByIdAsync(id);
        return assignment is null ? NotFound() : Ok(assignment);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveUserRoleAssignmentsRequest request)
    {
        try { return Ok(await assignments.SaveForUserAsync(request.UserId, request.Assignments)); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    [HttpPut("user/{userId:guid}")]
    public async Task<IActionResult> Update(
        Guid userId, [FromBody] SaveUserRoleAssignmentsRequest request)
    {
        if (request.UserId != Guid.Empty && request.UserId != userId)
            return BadRequest(new { message = "The route user and request user must match." });
        try { return Ok(await assignments.SaveForUserAsync(userId, request.Assignments)); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        try { await assignments.DeactivateAsync(id); return NoContent(); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    [HttpPost("bulk-deactivate")]
    public async Task<IActionResult> BulkDeactivate([FromBody] List<Guid> ids)
    {
        try { return Ok(new { deactivated = await assignments.BulkDeactivateAsync(ids ?? []) }); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }
}
