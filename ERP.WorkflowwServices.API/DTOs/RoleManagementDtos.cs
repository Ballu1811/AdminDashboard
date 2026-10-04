using System.ComponentModel.DataAnnotations;

namespace ERP.WorkflowwServices.API.DTOs
{
    public sealed class RoleListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystem { get; set; }
        public bool IsDefault { get; set; }
        public bool IsEditable { get; set; }
    }

    public sealed class SaveRoleRequest
    {
        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Code { get; set; }

        [StringLength(250)]
        public string? Description { get; set; }

        public int Priority { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class SetUserRolesRequest : IValidatableObject
    {
        public List<Guid> RoleIds { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RoleIds.Count == 0 || RoleIds.Any(roleId => roleId == Guid.Empty))
                yield return new ValidationResult("Select at least one valid role.", [nameof(RoleIds)]);
        }
    }
}
