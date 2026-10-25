using ERP.WorkflowwServices.API.Interfaces.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models;

[Table("tblSubModules")]
public class SubModule : BaseAuditableEntity, ITenantEntity
{
    [Key]
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    [Required]
    public Guid ParentId { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string ModuleKey { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Description { get; set; }
    public int OrderNo { get; set; }
    [MaxLength(100)]
    public string? Icon { get; set; } = "pi pi-box";
    [MaxLength(250)]
    public string? DefaultRoute { get; set; }
    [MaxLength(150)]
    public string? ComponentKey { get; set; }
    [MaxLength(100)]
    public string? Permission { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public bool IsLicensed { get; set; }
    public bool ShowInSidebar { get; set; } = true;
    public bool ShowInTopbar { get; set; }
    public Module? Parent { get; set; }
}
