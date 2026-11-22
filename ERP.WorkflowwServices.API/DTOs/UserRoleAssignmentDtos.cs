using System.ComponentModel.DataAnnotations;

namespace ERP.WorkflowwServices.API.DTOs;

public sealed class UserRoleAssignmentFilter
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? RoleId { get; set; }
    public string? Status { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }
}

public sealed class SaveUserRoleAssignmentsRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public List<UserRoleAssignmentInput> Assignments { get; set; } = [];
}

public sealed class UserRoleAssignmentInput
{
    public Guid? Id { get; set; }

    [Required]
    public Guid RoleId { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Remarks { get; set; }
}

public sealed class UserRoleAssignmentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool UserIsActive { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? EmployeeCode { get; set; }
    public long? PositionId { get; set; }
    public string? PositionName { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? RoleCode { get; set; }
    public string? RoleDescription { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public string AssignmentStatus { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public Guid? DeactivatedBy { get; set; }
}

public sealed class UserRoleAssignmentUserOptionDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? EmployeeCode { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public long? PositionId { get; set; }
    public string? PositionName { get; set; }
    public bool IsActive { get; set; }
}

public sealed class UserRoleAssignmentRoleOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public Guid TenantId { get; set; }
}

public sealed class UserRoleAssignmentDepartmentOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class UserRoleAssignmentSummaryDto
{
    public int UsersWithAssignedRoles { get; set; }
    public int ActiveAssignments { get; set; }
    public int UpcomingAssignments { get; set; }
    public int ExpiredAssignments { get; set; }
    public int InactiveAssignments { get; set; }
}

public sealed class UserRoleAssignmentHistoryDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Remarks { get; set; }
    public DateTime ChangedAt { get; set; }
    public Guid ChangedBy { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime? DeactivatedAt { get; set; }
    public Guid? DeactivatedBy { get; set; }
}
