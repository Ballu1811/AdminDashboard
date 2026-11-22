using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WorkflowwServices.API.Interfaces.Common;

namespace ERP.WorkflowwServices.API.Models;

[Table("tblPermissions")]
public sealed class Permission : BaseAuditableEntity, ITenantEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ModuleId { get; set; }

    public Guid? SubModuleId { get; set; }

    public Guid? MenuId { get; set; }

    [Required, MaxLength(150)]
    public string PermissionName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string PermissionKey { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string PermissionType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsSystemDefined { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(ModuleId))]
    public Module? Module { get; set; }

    [ForeignKey(nameof(SubModuleId))]
    public SubModule? SubModule { get; set; }

    [ForeignKey(nameof(MenuId))]
    public MenuItem? Menu { get; set; }

    public ICollection<RolePermission> RoleMappings { get; set; } = new List<RolePermission>();
}
