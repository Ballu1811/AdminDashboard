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
        [Required]
        [MaxLength(100)]
        public string ModuleKey { get; set; } = string.Empty;
        [Required]
        [MaxLength(50)]
        public string ModuleType { get; set; } = "Standard";
        [MaxLength(20)]
        public string? Version { get; set; } = "1.0.0";
        [MaxLength(100)]
        public string? Icon { get; set; }
        [MaxLength(250)]
        public string? DefaultRoute { get; set; }
        [MaxLength(150)]
        public string? ComponentKey { get; set; }
        [MaxLength(100)]
        public string? ThemeKey { get; set; }
        public int OrderNo { get; set; } = 0;
        public bool IsDefault { get; set; } = false; // default module after login
        public bool IsLicensed { get; set; } = true;
        public bool IsSystemModule { get; set; }
        public bool ShowInSidebar { get; set; } = true;
        public bool ShowInTopbar { get; set; }
        public Guid? ParentId { get; set; }
        public Module? ParentModule { get; set; }
        public ICollection<Module> ChildModules { get; set; } = new List<Module>();
        public string? Category { get; set; }
        [MaxLength(100)]
        public string? Permission { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        // optional description
        [MaxLength(500)]
        public string? Description { get; set; }
        // 🔗 Navigation (IMPORTANT)
        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
        public ICollection<SubModule> SubModules { get; set; } = new List<SubModule>();
    }
}
