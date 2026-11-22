using System.ComponentModel.DataAnnotations;
using ERP.WorkflowwServices.API.DTOs.FilterModels;

namespace ERP.WorkflowwServices.API.DTOs;

public sealed class PositionRoleDto
{
    public Guid RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class PositionDto
{
    public long Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public string PositionCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public long? ParentPositionId { get; set; }
    public string? ParentPositionName { get; set; }
    public int? PositionLevel { get; set; }
    public string PositionCategory { get; set; } = string.Empty;
    public bool IsManagerial { get; set; }
    public bool IsActive { get; set; }
    public string? Remarks { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AssignedRoleCount { get; set; }
    public List<PositionRoleDto> AssignedRoles { get; set; } = [];
}

public sealed class PositionSaveRequest : IValidatableObject
{
    public long? Id { get; set; }
    public Guid? CompanyId { get; set; }
    [Required, MaxLength(150)] public string PositionName { get; set; } = string.Empty;
    [Required, MaxLength(50), RegularExpression("^[A-Za-z0-9_-]+$")] public string PositionCode { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public Guid DepartmentId { get; set; }
    public long? ParentPositionId { get; set; }
    [Range(1, int.MaxValue)] public int? PositionLevel { get; set; }
    [Required, MaxLength(50)] public string PositionCategory { get; set; } = string.Empty;
    public bool IsManagerial { get; set; }
    public bool IsActive { get; set; } = true;
    [MaxLength(500)] public string? Remarks { get; set; }
    public List<Guid> RoleIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(PositionName))
            yield return new ValidationResult("Position name is required.", [nameof(PositionName)]);
        if (string.IsNullOrWhiteSpace(PositionCode))
            yield return new ValidationResult("Position code is required.", [nameof(PositionCode)]);
        if (string.IsNullOrWhiteSpace(PositionCategory) ||
            !new[] { "Executive", "Management", "Operational", "Administrative", "Technical" }.Contains(PositionCategory, StringComparer.OrdinalIgnoreCase))
            yield return new ValidationResult("Choose a valid position category.", [nameof(PositionCategory)]);
        if (DepartmentId == Guid.Empty) yield return new ValidationResult("Department is required.", [nameof(DepartmentId)]);
        if (RoleIds is null) yield return new ValidationResult("Role mappings are required.", [nameof(RoleIds)]);
        else if (RoleIds.Count != RoleIds.Distinct().Count())
            yield return new ValidationResult("Role mappings cannot contain duplicates.", [nameof(RoleIds)]);
    }
}

public sealed class PositionFilter : BasePaging
{
    public uint? Status { get; set; }
    public string? KeyWord { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public long? ParentPositionId { get; set; }
    public string? SortField { get; set; }
    public string? SortOrder { get; set; }
}

public sealed class PositionHierarchyDto : PositionDto
{
    public List<PositionHierarchyDto> Children { get; set; } = [];
}

public sealed record PositionDropdownDto(long Id, string PositionName, string PositionCode, Guid DepartmentId, long? ParentPositionId, int? PositionLevel, Guid? CompanyId);
public sealed record PositionCodeAvailabilityDto(bool IsAvailable);
