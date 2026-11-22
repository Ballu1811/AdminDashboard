using System.ComponentModel.DataAnnotations;

namespace ERP.WorkflowwServices.API.DTOs;

/// <summary>Active role available for permission mapping.</summary>
public sealed record RolePermissionRoleDto(Guid Id, string Name, string? Code, bool IsSystem);

/// <summary>Permission catalog item and its module hierarchy.</summary>
public sealed record RolePermissionItemDto(
    Guid Id,
    Guid ModuleId,
    string ModuleName,
    int ModuleOrder,
    Guid? SubModuleId,
    string? SubModuleName,
    int SubModuleOrder,
    Guid? MenuId,
    string? MenuName,
    int MenuOrder,
    string PermissionName,
    string PermissionKey,
    string PermissionType,
    int SortOrder,
    bool IsSystemDefined,
    bool IsGranted);

/// <summary>Complete permission mapping state for a role.</summary>
public sealed record RolePermissionMappingDto(
    Guid RoleId,
    string RoleName,
    IReadOnlyList<RolePermissionItemDto> Permissions);

/// <summary>One permission grant state in a full role mapping update.</summary>
public sealed record RolePermissionGrantRequest
{
    /// <summary>Stable permission catalog identifier.</summary>
    [Required]
    public Guid PermissionId { get; init; }

    /// <summary>Whether this role should have the permission.</summary>
    public bool IsGranted { get; init; }
}

/// <summary>Full desired permission state for a role.</summary>
public sealed record RolePermissionSaveRequest : IValidatableObject
{
    /// <summary>One grant state for every active permission in the loaded catalog.</summary>
    [Required]
    public required List<RolePermissionGrantRequest> Permissions { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Permissions is null)
            yield return new ValidationResult("Permission states are required.", [nameof(Permissions)]);
        else if (Permissions.Count > 5000)
            yield return new ValidationResult("A role mapping cannot exceed 5000 permission states.", [nameof(Permissions)]);
        else if (Permissions.Any(permission => permission.PermissionId == Guid.Empty))
            yield return new ValidationResult("Permission identifiers must be valid.", [nameof(Permissions)]);
        else if (Permissions.Select(permission => permission.PermissionId).Distinct().Count() != Permissions.Count)
            yield return new ValidationResult("A permission can only appear once in the mapping.", [nameof(Permissions)]);
    }
}
