using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WorkflowwServices.API.Interfaces.Common;

namespace ERP.WorkflowwServices.API.Models;

[Table("tblPositions")]
public sealed class Position : BaseAuditableEntity, ITenantEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    [Required, MaxLength(150)] public string PositionName { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string PositionCode { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public Guid DepartmentId { get; set; }
    public long? ParentPositionId { get; set; }
    public int? PositionLevel { get; set; }
    [Required, MaxLength(50)] public string PositionCategory { get; set; } = "Operational";
    public bool IsManagerial { get; set; }
    public bool IsActive { get; set; } = true;
    [MaxLength(500)] public string? Remarks { get; set; }
    public Position? ParentPosition { get; set; }
    public Department Department { get; set; } = null!;
    public ICollection<Position> ChildPositions { get; set; } = new List<Position>();
    public ICollection<PositionRole> PositionRoles { get; set; } = new List<PositionRole>();
}

[Table("tblPositionRoles")]
public sealed class PositionRole : BaseAuditableEntity, ITenantEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] public long Id { get; set; }
    public Guid TenantId { get; set; }
    public long PositionId { get; set; }
    public Guid RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public Position Position { get; set; } = null!;
    public Roles Role { get; set; } = null!;
}
