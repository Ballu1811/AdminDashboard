using ERP.WorkflowwServices.API.DTOs;

namespace ERP.WorkflowwServices.API.Interfaces;

public interface IUserPositionAssignmentService
{
    Task<IReadOnlyList<UserPositionAssignmentDto>> GetByUserAsync(Guid userId);
    Task<UserPositionAssignmentDto> AssignAsync(Guid userId, SaveUserPositionAssignmentRequest request);
}
