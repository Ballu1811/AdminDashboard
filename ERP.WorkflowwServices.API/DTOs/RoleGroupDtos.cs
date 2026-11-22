using System.ComponentModel.DataAnnotations;
using ERP.WorkflowwServices.API.DTOs.FilterModels;

namespace ERP.WorkflowwServices.API.DTOs;

/// <summary>Role details returned as a member of a role group.</summary>
public sealed record RoleGroupRoleDto
{
    public Guid RoleId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Code { get; init; }
    public string? Description { get; init; }
    public string? DepartmentName { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
}

/// <summary>A role group and its current role assignments.</summary>
public sealed record RoleGroupDto
{
    public long Id { get; init; }
    public Guid? CompanyId { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public string GroupCode { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public string? Remarks { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public Guid? UpdatedBy { get; init; }
    public int AssignedRoleCount { get; init; }
    public int ActiveAssignedRoleCount { get; init; }
    public int InactiveAssignedRoleCount { get; init; }
    public List<RoleGroupRoleDto> AssignedRoles { get; init; } = [];
}

/// <summary>Request data for creating or updating a role group.</summary>
public sealed record RoleGroupSaveRequest : IValidatableObject
{
    public Guid? CompanyId { get; init; }

    [Required, MaxLength(150)]
    public string GroupName { get; init; } = string.Empty;

    [Required, MaxLength(50), RegularExpression("^[A-Za-z0-9_-]+$")]
    public string GroupCode { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; init; }

    public Guid? DepartmentId { get; init; }

    [MaxLength(500)]
    public string? Remarks { get; init; }

    public bool IsActive { get; init; } = true;

    [MinLength(1)]
    public List<Guid> RoleIds { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(GroupName))
            yield return new ValidationResult("Group name is required.", [nameof(GroupName)]);
        if (string.IsNullOrWhiteSpace(GroupCode))
            yield return new ValidationResult("Group code is required.", [nameof(GroupCode)]);
        if (RoleIds is null || RoleIds.Count == 0 || RoleIds.Any(id => id == Guid.Empty))
            yield return new ValidationResult("Select at least one valid role.", [nameof(RoleIds)]);
        else if (RoleIds.Count != RoleIds.Distinct().Count())
            yield return new ValidationResult("Role mappings cannot contain duplicate roles.", [nameof(RoleIds)]);
    }
}

/// <summary>Pagination, status, company, department, and sorting filters for role groups.</summary>
public sealed class RoleGroupFilter : BasePaging
{
    public uint? Status { get; set; }
    public string? Search { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? SortField { get; set; }
    public string? SortOrder { get; set; }
}

/// <summary>A role group option available for selection in other modules.</summary>
public sealed record RoleGroupDropdownDto(long Id, string GroupName, string GroupCode, Guid? CompanyId);
