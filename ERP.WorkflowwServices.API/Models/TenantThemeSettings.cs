using ERP.WorkflowwServices.API.Interfaces.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblTenantThemeSettings")]
    public class TenantThemeSettings : BaseAuditableEntity, ITenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        /* ================= THEME ================= */
        [MaxLength(50)]
        public string Theme { get; set; } = "aura";

        public bool IsDarkMode { get; set; } = false;

        /* ================= COLORS ================= */
        [MaxLength(20)]
        public string? PrimaryColor { get; set; }

        [MaxLength(20)]
        public string? SurfaceColor { get; set; }

        [MaxLength(20)]
        public string? SidebarColor { get; set; }

        [MaxLength(20)]
        public string? TopbarColor { get; set; }

        /* ================= BRANDING ================= */
        [MaxLength(200)]
        public string? LogoUrl { get; set; }

        [MaxLength(200)]
        public string? FaviconUrl { get; set; }

        [MaxLength(100)]
        public string? CompanyName { get; set; }

        /* ================= DEFAULT UI ================= */
        [MaxLength(50)] 
        public string MenuType { get; set; } = "static";
        [MaxLength(20)] 
        public string Density { get; set; } = "default";

        public bool IsActive { get; set; } = true;
    }
}
