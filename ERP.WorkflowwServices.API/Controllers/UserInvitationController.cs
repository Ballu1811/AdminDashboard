using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.WorkflowwServices.API.Controllers;

[ApiController]
[Route("api/users/invitations")]
public sealed class UserInvitationController(IUserInvitationService invitations) : ControllerBase
{
    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<IActionResult> Accept(AcceptUserInvitationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await invitations.AcceptAsync(request, cancellationToken);
            return Ok(new
            {
                message = result.RequiresAdminApproval
                    ? "Invitation accepted. Your account is waiting for administrator approval."
                    : "Invitation accepted. You can now sign in.",
                result.RequiresAdminApproval
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
