using System.ComponentModel.DataAnnotations;
using ERP.WorkflowwServices.API.DTOs.FilterModels;

namespace ERP.WorkflowwServices.API.DTOs;

public sealed class DepartmentDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public string? ParentDepartmentName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ActiveChildCount { get; set; }
    public int ActivePositionCount { get; set; }
    public int ActiveUserCount { get; set; }
}

public sealed class DepartmentSaveRequest
{
    public Guid? CompanyId { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(50), RegularExpression("^[A-Za-z0-9_-]+$")] public string Code { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class DepartmentFilter : BasePaging
{
    public uint? Status { get; set; }
    public string? KeyWord { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public string? SortField { get; set; }
    public string? SortOrder { get; set; }
}

public sealed record DepartmentDropdownDto(Guid Id, string Name, string Code, Guid? ParentDepartmentId);
