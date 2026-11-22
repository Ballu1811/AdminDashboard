using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WorkflowwServices.API.Interfaces.Common;

namespace ERP.WorkflowwServices.API.Models;

[Table("tblCompanyUserSettings")]
public sealed class CompanyUserSettings : BaseAuditableEntity, ITenantEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid CompanyId { get; set; }

    public bool AllowUserRegistration { get; set; } = true;

    public bool AllowVendorUsers { get; set; } = true;

    public bool AllowCustomerUsers { get; set; } = true;

    public bool AllowExternalUsers { get; set; } = true;

    public bool RequireEmailVerification { get; set; } = true;

    public bool RequireAdminApproval { get; set; }

    public int? MaxActiveUsers { get; set; }

    [Range(1, 90)]
    public int InvitationExpiryDays { get; set; } = 7;

    public Company Company { get; set; } = null!;
}
