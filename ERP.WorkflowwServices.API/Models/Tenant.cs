using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Models;

[Table("tblTenants")]
[Index(nameof(Code), IsUnique = true)]
public sealed class Tenant : BaseAuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string TenantType { get; set; } = "Customer";

    public bool IsActive { get; set; } = true;
}
