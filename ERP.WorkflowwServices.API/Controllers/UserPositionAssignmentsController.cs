using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.WorkflowwServices.API.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/positions")]
[Authorize]
public sealed class UserPositionAssignmentsController(IUserPositionAssignmentService assignments) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "UserManagement.View")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        try
        {
            return Ok(await assignments.GetByUserAsync(userId));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [Authorize(Policy = "UserManagement.Update")]
    public async Task<IActionResult> Assign(Guid userId, SaveUserPositionAssignmentRequest request)
    {
        try
        {
            return Ok(await assignments.AssignAsync(userId, request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
