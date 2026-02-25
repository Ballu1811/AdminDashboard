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
        public ICollection<MenuItem>? Children { get; set; }
        public int OrderNo { get; set; } = 0;
        [MaxLength(100)]
        public string? Permission { get; set; }
        // menu behavior
        [MaxLength(50)]
        public string MenuType { get; set; } = "link";
        [MaxLength(150)]
        public string? Tooltip { get; set; }
        // external links
        public bool IsExternal { get; set; } = false;
        [MaxLength(20)]
        public string Target { get; set; } = "_self";
        // module/license control
        [MaxLength(100)]
        public string? Module { get; set; }
        public bool DefaultExpanded { get; set; } = false;
        // visibility
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
    }
}
