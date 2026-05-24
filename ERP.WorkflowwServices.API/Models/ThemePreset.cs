using ERP.WorkflowwServices.API.Interfaces.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblThemePresets")]
    public class ThemePreset : BaseAuditableEntity, ITenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PrimaryColor { get; set; } = string.Empty;

        [MaxLength(20)]
        public string SurfaceColor { get; set; } = string.Empty;

        [MaxLength(20)]
        public string SidebarColor { get; set; } = string.Empty;

        [MaxLength(20)]
        public string TopbarColor { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}
