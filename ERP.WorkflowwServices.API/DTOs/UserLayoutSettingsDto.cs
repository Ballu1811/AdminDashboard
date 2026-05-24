namespace ERP.WorkflowwServices.API.DTOs
{
    public class UserLayoutSettingsDto
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }

        /* 🔥 OVERRIDE FLAGS */
        public bool OverrideTheme { get; set; }
        public bool OverrideLayout { get; set; }

        public bool SidebarCollapsed { get; set; }
        public bool SidebarPinned { get; set; }
        public string? MenuType { get; set; }

        public string? Theme { get; set; }
        public bool IsDarkMode { get; set; }

        public string? PrimaryColor { get; set; }
        public string? SurfaceColor { get; set; }
        public string? SidebarColor { get; set; }
        public string? TopbarColor { get; set; }

        public string? Density { get; set; }
        public bool HeaderFixed { get; set; }
        public string? HeaderDensity { get; set; }

        public string? ThemePreset { get; set; }
        public bool UseSystemTheme { get; set; }
    }
}
