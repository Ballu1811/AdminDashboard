using ERP.WorkflowwServices.API.DTOs;

namespace ERP.WorkflowwServices.API.Interfaces;

public interface IUserInvitationService
{
    Task SendInvitationAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserInvitationAcceptedDto> AcceptAsync(
        AcceptUserInvitationRequest request,
        CancellationToken cancellationToken = default);
}
