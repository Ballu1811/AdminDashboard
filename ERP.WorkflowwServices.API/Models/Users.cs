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
        public Tenant Tenant { get; set; } = null!;

        // ================= BASIC =================
        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? DisplayName { get; set; }

        [MaxLength(80)]
        public string? FirstName { get; set; }

        [MaxLength(80)]
        public string? MiddleName { get; set; }

        [MaxLength(80)]
        public string? LastName { get; set; }

        [MaxLength(30)]
        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

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

        [MaxLength(30)]
        public string AuthenticationType { get; set; } = "Local";

        public bool RequireEmailVerification { get; set; } = true;

        public bool RequiresApproval { get; set; }

        public DateTime? AccountExpiresAt { get; set; }

        // ================= ROLE =================
        public Guid RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        public Roles? Role { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<UserRoleAssignment> UserRoleAssignments { get; set; } = new List<UserRoleAssignment>();
        public ICollection<UserCompanyAccess> CompanyAccesses { get; set; } = new List<UserCompanyAccess>();
        public ICollection<UserBranchAccess> BranchAccesses { get; set; } = new List<UserBranchAccess>();

        // ================= ORGANIZATION =================
        public Guid? CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public Company? Company { get; set; }

        public Guid? BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public Branch? Branch { get; set; }

        [MaxLength(50)]
        public string UserType { get; set; } = "Internal";

        public Guid? DepartmentId { get; set; }

        [MaxLength(50)]
        public string? EmployeeCode { get; set; }

        public long? PositionId { get; set; }

        [ForeignKey(nameof(PositionId))]
        public Position? Position { get; set; }

        public Guid? ReportingToUserId { get; set; }

        [ForeignKey(nameof(ReportingToUserId))]
        public Users? ReportingTo { get; set; }

        public ICollection<Users> Subordinates { get; set; } = new List<Users>();
        public DateTime? JoiningDate { get; set; }

        [MaxLength(50)]
        public string? EmploymentType { get; set; }

        [MaxLength(200)]
        public string? WorkLocation { get; set; }

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        public ICollection<UserPositionAssignment> PositionAssignments { get; set; } = new List<UserPositionAssignment>();
        public ICollection<UserInvitation> Invitations { get; set; } = new List<UserInvitation>();

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
