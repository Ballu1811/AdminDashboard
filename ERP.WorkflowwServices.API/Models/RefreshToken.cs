using ERP.WorkflowwServices.API.Interfaces.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblRefreshTokens")]
    public class RefreshToken : BaseAuditableEntity, ITenantEntity
    {
        [Key]
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public Users User { get; set; } = default!;
        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; }
        [MaxLength(250)]
        public string? ReplacedByToken { get; set; }

        [MaxLength(250)]
        public string? ReasonRevoked { get; set; }

        // 🔐 Optional (security tracking)
        [MaxLength(100)]
        public string? CreatedByIp { get; set; }

        [MaxLength(100)]
        public string? RevokedByIp { get; set; }
    }
}
