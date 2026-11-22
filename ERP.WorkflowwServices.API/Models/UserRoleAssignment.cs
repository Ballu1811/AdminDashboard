using ERP.WorkflowwServices.API.Interfaces.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models;

[Table("tblUserRoleAssignments")]
public sealed class UserRoleAssignment : BaseAuditableEntity, ITenantEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public Guid? CompanyId { get; set; }

    public bool IsDefault { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime? DeactivatedAt { get; set; }

    public Guid? DeactivatedBy { get; set; }

    [ForeignKey(nameof(UserId))]
    public Users? User { get; set; }

    [ForeignKey(nameof(RoleId))]
    public Roles? Role { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public Company? Company { get; set; }
}
