using System.ComponentModel.DataAnnotations;

namespace ERP.WorkflowwServices.API.DTOs;

/// <summary>Permission categories supported by the catalog.</summary>
public static class PermissionTypeCatalog
{
    /// <summary>Gets the canonical permission type names.</summary>
    public static IReadOnlyList<PermissionTypeDto> Types { get; } =
    [
        new("View", "View"),
        new("Create", "Create"),
        new("Edit", "Edit"),
        new("Delete", "Delete"),
        new("Approve", "Approve"),
        new("Reject", "Reject"),
        new("Export", "Export"),
        new("Import", "Import"),
        new("Print", "Print"),
        new("Archive", "Archive"),
        new("Restore", "Restore"),
        new("Custom", "Custom")
    ];

    /// <summary>Normalizes a supported type or throws a validation error.</summary>
    public static string Normalize(string? value)
    {
        var type = Types.FirstOrDefault(item =>
            string.Equals(item.Value, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        return type?.Value ?? throw new InvalidOperationException("Select a supported permission type.");
    }
}

/// <summary>Permission returned by create and update operations.</summary>
public sealed record PermissionDto(
    Guid Id,
    Guid ModuleId,
    Guid? SubModuleId,
    Guid? MenuId,
    string PermissionName,
    string PermissionKey,
    string PermissionType,
    string? Description,
    int SortOrder,
    bool IsSystemDefined,
    bool IsActive,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    string? ModuleName,
    string? SubModuleName,
    string? MenuName);

/// <summary>Permission row returned by the paginated catalog endpoint.</summary>
public sealed record PermissionListDto(
    Guid Id,
    Guid ModuleId,
    Guid? SubModuleId,
    Guid? MenuId,
    string PermissionName,
    string PermissionKey,
    string PermissionType,
    string? Description,
    int SortOrder,
    bool IsSystemDefined,
    bool IsActive,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    string? ModuleName,
    string? SubModuleName,
    string? MenuName);

/// <summary>Permission details, including creation, update, and archive audit fields.</summary>
public sealed record PermissionDetailsDto(
    Guid Id,
    Guid ModuleId,
    Guid? SubModuleId,
    Guid? MenuId,
    string PermissionName,
    string PermissionKey,
    string PermissionType,
    string? Description,
    int SortOrder,
    bool IsSystemDefined,
    bool IsActive,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy,
    DateTimeOffset? DeletedAt,
    Guid? DeletedBy,
    string? ModuleName,
    string? SubModuleName,
    string? MenuName);

/// <summary>Payload for creating or updating a permission.</summary>
public sealed record PermissionSaveRequest
{
    /// <summary>Module that owns the permission.</summary>
    [Required]
    public Guid ModuleId { get; init; }

    /// <summary>Optional submodule within the selected module.</summary>
    public Guid? SubModuleId { get; init; }

    /// <summary>Optional menu within the selected module.</summary>
    public Guid? MenuId { get; init; }

    /// <summary>Display name of the permission.</summary>
    [Required, MaxLength(150)]
    public required string PermissionName { get; init; }

    /// <summary>Stable permission key; normalized to uppercase by the API.</summary>
    [Required, MaxLength(150)]
    public required string PermissionKey { get; init; }

    /// <summary>Supported permission type.</summary>
    [Required, MaxLength(50)]
    public required string PermissionType { get; init; }

    /// <summary>Optional permission description.</summary>
    [MaxLength(500)]
    public string? Description { get; init; }

    /// <summary>Non-negative display order.</summary>
    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }

    /// <summary>Whether the permission is available for use.</summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>Filters and paging options for the permission catalog.</summary>
public sealed record PermissionFilter
{
    /// <summary>One-based page number.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>Number of rows per page.</summary>
    public int PageSize { get; init; } = 20;

    /// <summary>Searches permission name, key, and description.</summary>
    public string? Search { get; init; }

    /// <summary>0=active, 1=archived, 2=inactive, null=all.</summary>
    public int? Status { get; init; }

    /// <summary>Optional module filter.</summary>
    public Guid? ModuleId { get; init; }

    /// <summary>Optional submodule filter.</summary>
    public Guid? SubModuleId { get; init; }

    /// <summary>Optional menu filter.</summary>
    public Guid? MenuId { get; init; }

    /// <summary>Optional permission type filter.</summary>
    public string? PermissionType { get; init; }

    /// <summary>Allow-listed sort field.</summary>
    public string? SortField { get; init; } = "permissionName";

    /// <summary>asc or desc.</summary>
    public string? SortOrder { get; init; } = "asc";
}

/// <summary>Active permission item for consumers that need a permission selector.</summary>
public sealed record PermissionDropdownDto(Guid Id, string PermissionName, string PermissionKey, string PermissionType);

/// <summary>Permission type value and display label.</summary>
public sealed record PermissionTypeDto(string Value, string Label);
