using System.ComponentModel.DataAnnotations;

namespace ERP.WorkflowwServices.API.DTOs;

public class SubModuleDto
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderNo { get; set; }
    public string? Icon { get; set; }
    public string? DefaultRoute { get; set; }
    public string? ComponentKey { get; set; }
    public string? Permission { get; set; }
    public bool IsActive { get; set; }
    public bool IsVisible { get; set; }
    public bool IsLicensed { get; set; }
    public bool ShowInSidebar { get; set; }
    public bool ShowInTopbar { get; set; }
    public bool IsDeleted { get; set; }
}

public class SubModuleCreateDto
{
    [Required] public Guid ParentId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(50), RegularExpression("^[A-Za-z0-9_-]+$")] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(100), RegularExpression("^[a-z0-9_-]+$")] public string ModuleKey { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    [Range(0, int.MaxValue)] public int OrderNo { get; set; }
    [MaxLength(100)] public string? Icon { get; set; } = "pi pi-box";
    [MaxLength(250)] public string? DefaultRoute { get; set; }
    [MaxLength(150)] public string? ComponentKey { get; set; }
    [MaxLength(100)] public string? Permission { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public bool IsLicensed { get; set; }
    public bool ShowInSidebar { get; set; } = true;
    public bool ShowInTopbar { get; set; }
}

public sealed class SubModuleUpdateDto : SubModuleCreateDto
{
    [Required] public Guid Id { get; set; }
}
