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
        public string LegalName { get; set; } = string.Empty;
        public string CompanyType { get; set; } = "Private Limited";
        public string? Industry { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? PanNumber { get; set; }
        public string? Gstin { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PrimaryContactName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? TaxNumber { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string BaseCurrency { get; set; } = "INR";
        public string TimeZoneId { get; set; } = "Asia/Kolkata";
        public int FiscalYearStartMonth { get; set; } = 4;
        public int FiscalYearStartDay { get; set; } = 1;
        public string Locale { get; set; } = "en-IN";
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

        [Required, StringLength(150)]
        public string LegalName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string CompanyType { get; set; } = "Private Limited";

        [StringLength(100)]
        public string? Industry { get; set; }

        [StringLength(50)]
        public string? RegistrationNumber { get; set; }

        [StringLength(20)]
        public string? PanNumber { get; set; }

        [StringLength(20)]
        public string? Gstin { get; set; }

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string PrimaryContactName { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(200), Url]
        public string? Website { get; set; }

        [StringLength(50)]
        public string? TaxNumber { get; set; }

        [Required, StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [Required, StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string State { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Country { get; set; } = string.Empty;

        [Required, StringLength(3, MinimumLength = 3)]
        public string BaseCurrency { get; set; } = "INR";

        [Required, StringLength(100)]
        public string TimeZoneId { get; set; } = "Asia/Kolkata";

        [Range(1, 12)]
        public int FiscalYearStartMonth { get; set; } = 4;

        [Range(1, 31)]
        public int FiscalYearStartDay { get; set; } = 1;

        [Required, StringLength(20)]
        public string Locale { get; set; } = "en-IN";

        [Required, StringLength(50)]
        public string AccessLevel { get; set; } = "Custom";

        public bool IsActive { get; set; } = true;
        public List<Guid> ModuleIds { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Company name is required.", [nameof(Name)]);
            if (string.IsNullOrWhiteSpace(Code))
                yield return new ValidationResult("Company code is required.", [nameof(Code)]);
            if (string.IsNullOrWhiteSpace(LegalName))
                yield return new ValidationResult("Legal name is required.", [nameof(LegalName)]);
            var requiredFields = new[]
            {
                (Value: CompanyType, Field: nameof(CompanyType), Label: "Company type"),
                (Value: Email, Field: nameof(Email), Label: "Email"),
                (Value: PrimaryContactName, Field: nameof(PrimaryContactName), Label: "Primary contact name"),
                (Value: Phone, Field: nameof(Phone), Label: "Phone"),
                (Value: Address, Field: nameof(Address), Label: "Registered address"),
                (Value: City, Field: nameof(City), Label: "City"),
                (Value: State, Field: nameof(State), Label: "State or province"),
                (Value: PostalCode, Field: nameof(PostalCode), Label: "Postal code"),
                (Value: Country, Field: nameof(Country), Label: "Country"),
                (Value: BaseCurrency, Field: nameof(BaseCurrency), Label: "Base currency"),
                (Value: TimeZoneId, Field: nameof(TimeZoneId), Label: "Time zone"),
                (Value: Locale, Field: nameof(Locale), Label: "Locale")
            };
            foreach (var requiredField in requiredFields)
            {
                if (string.IsNullOrWhiteSpace(requiredField.Value))
                    yield return new ValidationResult($"{requiredField.Label} is required.", [requiredField.Field]);
            }
            if (FiscalYearStartMonth is >= 1 and <= 12 && FiscalYearStartDay is >= 1 and <= 31 &&
                FiscalYearStartDay > DateTime.DaysInMonth(2000, FiscalYearStartMonth))
                yield return new ValidationResult("Fiscal year start date is invalid.", [nameof(FiscalYearStartDay)]);
            if (ModuleIds.Any(moduleId => moduleId == Guid.Empty))
                yield return new ValidationResult("Module selections must be valid.", [nameof(ModuleIds)]);
        }
    }
}
