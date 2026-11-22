using System.ComponentModel.DataAnnotations;

namespace ERP.WorkflowwServices.API.DTOs;

public sealed class AcceptUserInvitationRequest
{
    [Required, MinLength(32), MaxLength(128)]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(72)]
    public string Password { get; set; } = string.Empty;
}

public sealed record UserInvitationAcceptedDto(bool RequiresAdminApproval);
