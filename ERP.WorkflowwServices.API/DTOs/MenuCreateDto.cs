namespace ERP.WorkflowwServices.API.DTOs
{

    public class MenuDto
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = string.Empty;

        // ================= BASIC =================
        public string? Route { get; set; }
        public string? Icon { get; set; }
        public Guid? ParentId { get; set; }
        public Guid? ModuleId { get; set; }

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
        public string Label { get; set; } = string.Empty;
        public string? Route { get; set; }
        public string? Icon { get; set; }
        public Guid? ParentId { get; set; }
        public Guid? ModuleId { get; set; }

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

        public bool ShowInSidebar { get; set; }
        public bool ShowInTopbar { get; set; }

        public bool IsDefault { get; set; }
        public bool IsHidden { get; set; }
        public bool IsSystem { get; set; }
        
        public bool DefaultExpanded { get; set; }   
    }

    public class MenuCreateDto
    {
        public string Label { get; set; } = string.Empty;

        // ================= BASIC =================
        public string? Route { get; set; }
        public string? Icon { get; set; }
        public Guid? ParentId { get; set; }
        public Guid? ModuleId { get; set; }

        public int OrderNo { get; set; }

        // ================= BEHAVIOR =================
        public string MenuType { get; set; } = "link";
        public string? Permission { get; set; }
        public string? Tooltip { get; set; }

        // ================= NAVIGATION =================
        public bool IsExternal { get; set; }
        public string? ExternalUrl { get; set; }
        public string Target { get; set; } = "_self";
        public string? QueryParams { get; set; }

        // ================= ACCESS =================
        public List<Guid>? RoleIds { get; set; } = new();

        // ================= UI =================
        public string? Badge { get; set; }
        public string? Color { get; set; }
        public string? CssClass { get; set; }

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
