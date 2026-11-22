using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WorkflowwServices.API.Interfaces.Common;

namespace ERP.WorkflowwServices.API.Models;

[Table("tblUserPositionAssignments")]
public sealed class UserPositionAssignment : BaseAuditableEntity, ITenantEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid UserId { get; set; }

    public long PositionId { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Remarks { get; set; }

    [ForeignKey(nameof(UserId))]
    public Users User { get; set; } = null!;

    [ForeignKey(nameof(PositionId))]
    public Position Position { get; set; } = null!;
}
