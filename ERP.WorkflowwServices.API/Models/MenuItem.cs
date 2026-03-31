using ERP.WorkflowwServices.API.Interfaces.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblMenuItem")]
    public class MenuItem : BaseAuditableEntity, ITenantEntity
    {
        [Key]
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        // ================= BASIC =================
        [Required]
        [MaxLength(150)]
        public string Label { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? Icon { get; set; }
        [MaxLength(250)]
        public string? Route { get; set; }
        public Guid? ParentId { get; set; }
        [ForeignKey(nameof(ParentId))]
        public MenuItem? Parent { get; set; }      
        public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
        public int OrderNo { get; set; } = 0;
        public Guid? ModuleId { get; set; }
        [ForeignKey(nameof(ModuleId))]
        public Module? ModuleMaster { get; set; }

        // ================= BEHAVIOR =================
        [MaxLength(100)]
        public string? Permission { get; set; }    
        [MaxLength(50)]
        public string MenuType { get; set; } = "link";
        [MaxLength(150)]
        public string? Tooltip { get; set; }

        // ================= NAVIGATION =================
        public bool IsExternal { get; set; } = false;
        [MaxLength(500)]
        public string? ExternalUrl { get; set; }
        [MaxLength(20)]
        public string Target { get; set; } = "_self";
        [MaxLength(500)]
        public string? QueryParams { get; set; }

        // ================= ACCESS =================
        public ICollection<MenuRole> MenuRoles { get; set; } = new List<MenuRole>();

        // ================= UI SETTINGS =================
        [MaxLength(50)]
        public string? Badge { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }
        [MaxLength(100)]
        public string? CssClass { get; set; }

        // ================= FLAGS =================
        public bool IsActive { get; set; } = true;

        public bool IsVisible { get; set; } = true;

        public bool ShowInSidebar { get; set; } = true;

        public bool ShowInTopbar { get; set; } = false;

        public bool IsDefault { get; set; } = false;

        public bool IsHidden { get; set; } = false;

        public bool IsSystem { get; set; } = false;

        public bool DefaultExpanded { get; set; } = false;        
    }
}
