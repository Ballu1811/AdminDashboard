using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WorkflowwServices.API.Interfaces.Common;

namespace ERP.WorkflowwServices.API.Models;

[Table("tblUserBranchAccess")]
public sealed class UserBranchAccess : BaseAuditableEntity, ITenantEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid UserId { get; set; }

    public Guid BranchId { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDefault { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    [ForeignKey(nameof(UserId))]
    public Users User { get; set; } = null!;

    [ForeignKey(nameof(BranchId))]
    public Branch Branch { get; set; } = null!;
}
