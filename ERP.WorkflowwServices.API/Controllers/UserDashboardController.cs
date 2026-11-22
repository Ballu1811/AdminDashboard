using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.WorkflowwServices.API.Controllers;

[ApiController]
[Route("api/user-dashboard")]
[Authorize(Policy = "UserManagement.View")]
public sealed class UserDashboardController(IUserDashboardService dashboard) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<UserDashboardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDashboardResponse>> Get(
        [FromQuery] UserDashboardFilter filter,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await dashboard.GetDashboardAsync(filter, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid dashboard filter",
                Detail = exception.Message,
                Instance = HttpContext.Request.Path
            });
        }
    }
}
