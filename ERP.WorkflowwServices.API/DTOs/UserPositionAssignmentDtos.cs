namespace ERP.WorkflowwServices.API.DTOs;

public sealed record UserPositionAssignmentDto(
    Guid Id,
    Guid UserId,
    long PositionId,
    string PositionName,
    Guid DepartmentId,
    string DepartmentName,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    bool IsActive,
    string? Remarks);

public sealed record SaveUserPositionAssignmentRequest(
    long PositionId,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    string? Remarks);
