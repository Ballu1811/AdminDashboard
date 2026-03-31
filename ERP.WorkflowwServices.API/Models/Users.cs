using ERP.WorkflowwServices.API.Interfaces.Common;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblUsers")]
    [Index(nameof(Username), nameof(TenantId), IsUnique = true)]
    [Index(nameof(Email), nameof(TenantId), IsUnique = true)]
    public class Users : BaseAuditableEntity, ITenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        // 🔥 Multi-tenant
        public Guid TenantId { get; set; }

        // ================= BASIC =================
        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? DisplayName { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? MobileNo { get; set; }

        // ================= AUTH =================
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string? PasswordSalt { get; set; }

        public bool IsEmailVerified { get; set; } = false;

        public bool IsMobileVerified { get; set; } = false;

        // ================= ROLE =================
        public Guid RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        public Roles? Role { get; set; }

        // ================= ORGANIZATION =================
        public Guid? DepartmentId { get; set; }

        public Guid? ReportingToUserId { get; set; }

        [ForeignKey(nameof(ReportingToUserId))]
        public Users? ReportingTo { get; set; }

        public ICollection<Users> Subordinates { get; set; } = new List<Users>();

        // ================= SECURITY =================
        public int AccessFailedCount { get; set; } = 0;

        public bool IsLocked { get; set; } = false;

        public DateTime? LockoutEnd { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public string? LastLoginIp { get; set; }

        // ================= FLAGS =================
        public bool IsActive { get; set; } = true;

        public bool IsSystem { get; set; } = false;

        public bool MustChangePassword { get; set; } = false;

        // ================= UI =================
        [MaxLength(50)]
        public string? Theme { get; set; }

        [MaxLength(50)]
        public string? TimeZone { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; }

        // ================= EXTRA =================
        [MaxLength(250)]
        public string? ProfileImageUrl { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }

        public bool TwoFactorEnabled { get; set; }
        public string? OtpSecret { get; set; }
    }
}
