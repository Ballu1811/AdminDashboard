using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WorkflowwServices.API.Interfaces.Common;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblCompanies")]
    [Index(nameof(Code), IsUnique = true)]
    public sealed class Company : BaseAuditableEntity, ITenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }
        public Guid? ParentCompanyId { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string LegalName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string CompanyType { get; set; } = "Private Limited";

        [MaxLength(100)]
        public string? Industry { get; set; }

        [MaxLength(50)]
        public string? RegistrationNumber { get; set; }

        [MaxLength(20)]
        public string? PanNumber { get; set; }

        [MaxLength(20)]
        public string? Gstin { get; set; }

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string PrimaryContactName { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Website { get; set; }

        [MaxLength(50)]
        public string? TaxNumber { get; set; }

        [Required, MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? AddressLine2 { get; set; }

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string State { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        [Required, MaxLength(3)]
        public string BaseCurrency { get; set; } = "INR";

        [Required, MaxLength(100)]
        public string TimeZoneId { get; set; } = "Asia/Kolkata";

        [Range(1, 12)]
        public int FiscalYearStartMonth { get; set; } = 4;

        [Range(1, 31)]
        public int FiscalYearStartDay { get; set; } = 1;

        [Required, MaxLength(20)]
        public string Locale { get; set; } = "en-IN";

        [MaxLength(50)]
        public string AccessLevel { get; set; } = "Custom";

        public bool IsActive { get; set; } = true;

        public ICollection<CompanyModuleAccess> ModuleAccess { get; set; } = new List<CompanyModuleAccess>();
        public Tenant Tenant { get; set; } = null!;
        public Company? ParentCompany { get; set; }
        public ICollection<Company> ChildCompanies { get; set; } = new List<Company>();
        public ICollection<UserCompanyAccess> UserAccesses { get; set; } = new List<UserCompanyAccess>();
    }
}
