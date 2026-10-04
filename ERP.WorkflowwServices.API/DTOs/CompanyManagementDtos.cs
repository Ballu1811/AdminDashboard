using System.ComponentModel.DataAnnotations;

namespace ERP.WorkflowwServices.API.DTOs
{
    public sealed class CompanyModuleOptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Category { get; set; }
    }

    public sealed class CompanyListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? LegalName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? TaxNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string AccessLevel { get; set; } = "Custom";
        public bool IsActive { get; set; }
        public List<Guid> ModuleIds { get; set; } = new();
        public List<string> ModuleNames { get; set; } = new();
    }

    public sealed class SaveCompanyRequest : IValidatableObject
    {
        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(150)]
        public string? LegalName { get; set; }

        [EmailAddress, StringLength(150)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(200), Url]
        public string? Website { get; set; }

        [StringLength(50)]
        public string? TaxNumber { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [Required, StringLength(50)]
        public string AccessLevel { get; set; } = "Custom";

        public bool IsActive { get; set; } = true;
        public List<Guid> ModuleIds { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ModuleIds.Any(moduleId => moduleId == Guid.Empty))
                yield return new ValidationResult("Module selections must be valid.", [nameof(ModuleIds)]);
        }
    }
}
