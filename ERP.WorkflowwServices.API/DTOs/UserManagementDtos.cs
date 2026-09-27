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
        public Guid RoleId { get; set; }
        public string? RoleName { get; set; }
        public Guid? DepartmentId { get; set; }
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

        public Guid RoleId { get; set; }

        [Required, MinLength(8), MaxLength(72)]
        public string Password { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RoleId == Guid.Empty)
                yield return new ValidationResult("A role is required.", [nameof(RoleId)]);
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

        public Guid RoleId { get; set; }

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