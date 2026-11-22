using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WorkflowwServices.API.Interfaces.Common;

namespace ERP.WorkflowwServices.API.Models;

[Table("tblRoleGroups")]
public sealed class RoleGroup : BaseAuditableEntity, ITenantEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public Guid TenantId { get; set; }
    public Guid? CompanyId { get; set; }

    [Required, MaxLength(150)]
    public string GroupName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string NormalizedGroupName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string GroupCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public Guid? DepartmentId { get; set; }
    [MaxLength(500)]
    public string? Remarks { get; set; }
    public bool IsActive { get; set; } = true;

    public Department? Department { get; set; }
    public ICollection<RoleGroupMapping> RoleMappings { get; set; } = new List<RoleGroupMapping>();
}

[Table("tblRoleGroupMappings")]
public sealed class RoleGroupMapping : BaseAuditableEntity, ITenantEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public Guid TenantId { get; set; }
    public long RoleGroupId { get; set; }
    public Guid RoleId { get; set; }
    public bool IsActive { get; set; } = true;

    public RoleGroup RoleGroup { get; set; } = null!;
    public Roles Role { get; set; } = null!;
}
