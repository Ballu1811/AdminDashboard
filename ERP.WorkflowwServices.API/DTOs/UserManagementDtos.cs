using System.ComponentModel.DataAnnotations;

namespace ERP.WorkflowwServices.API.DTOs
{
    public sealed class UserListDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? MobileNo { get; set; }
        public Guid TenantId { get; set; }
        public string? TenantName { get; set; }
        public Guid RoleId { get; set; }
        public string? RoleName { get; set; }
        public List<Guid> RoleIds { get; set; } = new();
        public List<string> RoleNames { get; set; } = new();
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? EmployeeCode { get; set; }
        public long? PositionId { get; set; }
        public string? PositionName { get; set; }
        public Guid? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public List<Guid> CompanyAccessIds { get; set; } = new();
        public List<string> CompanyAccessNames { get; set; } = new();
        public Guid? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string UserType { get; set; } = "Internal";
        public bool InvitationPending { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public sealed class UserRoleOptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
    }

    public sealed class UserCompanyOptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed record UserBranchOptionDto(Guid Id, string Name, string Code, Guid CompanyId);
    public sealed record UserTypeOptionDto(string Value, string Label);

    public sealed class CreateUserRequest : IValidatableObject
    {
        [Required, StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? MobileNo { get; set; }

        [MaxLength(50)]
        public string? EmployeeCode { get; set; }

        public Guid? DepartmentId { get; set; }

        public long? PositionId { get; set; }

        public Guid RoleId { get; set; }

        public Guid? CompanyId { get; set; }
        public List<Guid>? CompanyIds { get; set; }
        public Guid? BranchId { get; set; }

        [MaxLength(50)]
        public string UserType { get; set; } = "Internal";

        [MaxLength(72)]
        public string Password { get; set; } = string.Empty;

        public bool SendInvitation { get; set; }

        public bool IsActive { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RoleId == Guid.Empty)
                yield return new ValidationResult("A role is required.", [nameof(RoleId)]);
            if (!SendInvitation && Password.Length is < 8 or > 72)
                yield return new ValidationResult("Password must contain 8–72 characters unless an invitation is sent.", [nameof(Password)]);
        }
    }

    public sealed class UpdateUserRequest : IValidatableObject
    {
        [Required, StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? MobileNo { get; set; }

        [MaxLength(50)]
        public string? EmployeeCode { get; set; }

        public Guid? DepartmentId { get; set; }

        public long? PositionId { get; set; }

        public Guid RoleId { get; set; }

        public Guid? CompanyId { get; set; }
        public List<Guid>? CompanyIds { get; set; }

        public Guid? BranchId { get; set; }

        [MaxLength(50)]
        public string UserType { get; set; } = "Internal";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RoleId == Guid.Empty)
                yield return new ValidationResult("A role is required.", [nameof(RoleId)]);
        }
    }

    public sealed class SetUserStatusRequest
    {
        public bool IsActive { get; set; }
    }
}