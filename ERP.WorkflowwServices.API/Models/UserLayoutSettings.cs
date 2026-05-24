using ERP.WorkflowwServices.API.Interfaces.Common;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblUserLayoutSettings")]
    [Index(nameof(UserId), nameof(TenantId), IsUnique = true)]
    public class UserLayoutSettings : BaseAuditableEntity, ITenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        /* ================= OVERRIDE FLAGS ================= */
        public bool OverrideTheme { get; set; } = false;
        public bool OverrideLayout { get; set; } = false;

        /* ================= SIDEBAR ================= */
        public bool SidebarCollapsed { get; set; } = false;
        public bool SidebarPinned { get; set; } = true;

        [MaxLength(50)]
        public string MenuType { get; set; } = "static"; // static, slim, slim-plus, drawer, etc.

        /* ================= THEME ================= */
        [MaxLength(50)]
        public string Theme { get; set; } = "aura"; // primeng preset

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

        /* ================= UI ================= */
        [MaxLength(20)]
        public string Density { get; set; } = "default"; // compact, comfortable

        public bool HeaderFixed { get; set; } = true;

        [MaxLength(20)]
        public string HeaderDensity { get; set; } = "default";

        /* ================= ADVANCED ================= */
        [MaxLength(50)]
        public string? ThemePreset { get; set; }

        public bool UseSystemTheme { get; set; } = false;

        public bool IsActive { get; set; } = true;

        /* 🔗 Navigation */
        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }
    }
}
