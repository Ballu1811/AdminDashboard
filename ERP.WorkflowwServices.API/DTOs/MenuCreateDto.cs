using System.ComponentModel.DataAnnotations;

namespace ERP.WorkflowwServices.API.DTOs
{

    public class SidebarModuleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? DefaultRoute { get; set; }
        public string? ComponentKey { get; set; }
        public string? Category { get; set; }
        public int OrderNo { get; set; }
    }

    public class MenuDto
    {
        public Guid Id { get; set; }
        public Guid? MenuItemId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ComponentKey { get; set; }
        public bool IsLicensed { get; set; }

        // ================= BASIC =================
        public string? Route { get; set; }
        public string? Icon { get; set; }
        public Guid? ParentId { get; set; }
        public Guid? ModuleId { get; set; }
        public string? ModuleName { get; set; }
        public string? ParentMenuName { get; set; }
        public int OrderNo { get; set; }

        // ================= BEHAVIOR =================
        public string? MenuType { get; set; }
        public string? Permission { get; set; }
        public string? Tooltip { get; set; }

        // ================= NAVIGATION =================
        public bool IsExternal { get; set; }
        public string? ExternalUrl { get; set; }
        public string? Target { get; set; }
        public string? QueryParams { get; set; }

        // ================= ACCESS =================
        public List<Guid>? RoleIds { get; set; } = new();

        // ================= UI =================
        public string? Badge { get; set; }
        public string? Color { get; set; }
        public string? CssClass { get; set; }

        // ================= FLAGS =================
        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }
        public bool IsDeleted { get; set; }

        public bool ShowInSidebar { get; set; }
        public bool ShowInTopbar { get; set; }

        public bool IsDefault { get; set; }
        public bool IsHidden { get; set; }
        public bool IsSystem { get; set; }

        public bool DefaultExpanded { get; set; }

        // ================= HIERARCHY =================
        public List<MenuDto> Children { get; set; } = new();
    }

    public class MenuUpdateDto
    {
        public Guid Id { get; set; }

        // ================= BASIC =================
        [Required, MaxLength(100)] public string Label { get; set; } = string.Empty;
        [MaxLength(500)] public string? Description { get; set; }
        [MaxLength(150)] public string? ComponentKey { get; set; }
        public bool IsLicensed { get; set; }
        [MaxLength(250)] public string? Route { get; set; }
        [MaxLength(100)] public string? Icon { get; set; }
        public Guid? ParentId { get; set; }
        [Required]
        public Guid? ModuleId { get; set; }
        [Range(0, int.MaxValue)]
        public int OrderNo { get; set; }

        // ================= BEHAVIOR =================
        [MaxLength(50)] public string? MenuType { get; set; }
        [MaxLength(100)] public string? Permission { get; set; }
        [MaxLength(150)] public string? Tooltip { get; set; }

        // ================= NAVIGATION =================
        public bool IsExternal { get; set; }
        [MaxLength(500)] public string? ExternalUrl { get; set; }
        [MaxLength(20)] public string? Target { get; set; }
        [MaxLength(500)] public string? QueryParams { get; set; }

        // ================= ACCESS =================
        public List<Guid>? RoleIds { get; set; } = new();

        // ================= UI =================
        [MaxLength(50)] public string? Badge { get; set; }
        [MaxLength(50)] public string? Color { get; set; }
        [MaxLength(100)] public string? CssClass { get; set; }

        // ================= FLAGS =================
        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }

        public bool ShowInSidebar { get; set; }
        public bool ShowInTopbar { get; set; }

        public bool IsDefault { get; set; }
        public bool IsHidden { get; set; }
        public bool IsSystem { get; set; }
        
        public bool DefaultExpanded { get; set; }   
    }

    public class MenuCreateDto
    {
        [Required, MaxLength(100)] public string Label { get; set; } = string.Empty;
        [MaxLength(500)] public string? Description { get; set; }
        [MaxLength(150)] public string? ComponentKey { get; set; }
        public bool IsLicensed { get; set; }

        // ================= BASIC =================
        [MaxLength(250)] public string? Route { get; set; }
        [MaxLength(100)] public string? Icon { get; set; }
        public Guid? ParentId { get; set; }
        [Required]
        public Guid? ModuleId { get; set; }
        [Range(0, int.MaxValue)]
        public int OrderNo { get; set; }

        // ================= BEHAVIOR =================
        [MaxLength(50)] public string MenuType { get; set; } = "link";
        [MaxLength(100)] public string? Permission { get; set; }
        [MaxLength(150)] public string? Tooltip { get; set; }

        // ================= NAVIGATION =================
        public bool IsExternal { get; set; }
        [MaxLength(500)] public string? ExternalUrl { get; set; }
        [MaxLength(20)] public string Target { get; set; } = "_self";
        [MaxLength(500)] public string? QueryParams { get; set; }

        // ================= ACCESS =================
        public List<Guid>? RoleIds { get; set; } = new();

        // ================= UI =================
        [MaxLength(50)] public string? Badge { get; set; }
        [MaxLength(50)] public string? Color { get; set; }
        [MaxLength(100)] public string? CssClass { get; set; }

        // ================= FLAGS =================
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;

        public bool ShowInSidebar { get; set; } = true;
        public bool ShowInTopbar { get; set; } = false;

        public bool IsDefault { get; set; }
        public bool IsHidden { get; set; }
        public bool IsSystem { get; set; }

        public bool DefaultExpanded { get; set; }      
    }
}
