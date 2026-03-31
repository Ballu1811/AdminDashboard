using ERP.WorkflowwServices.API.Interfaces.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblModules")]
    public class Module: BaseAuditableEntity, ITenantEntity
    {
        [Key]
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? Icon { get; set; }
        [MaxLength(250)]
        public string? DefaultRoute { get; set; }
        public int OrderNo { get; set; } = 0;
        public bool IsDefault { get; set; } = false; // default module after login
        public bool IsLicensed { get; set; } = true;
        public string? Category { get; set; }
        [MaxLength(100)]
        public string? Permission { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        // optional description
        [MaxLength(250)]
        public string? Description { get; set; }
        // 🔗 Navigation (IMPORTANT)
        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}
