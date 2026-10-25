using System.ComponentModel.DataAnnotations;

namespace ERP.WorkflowwServices.API.DTOs
{
    public class ModuleDto
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string ModuleKey { get; set; } = string.Empty;
        public string ModuleType { get; set; } = "Standard";
        public string? Version { get; set; }
        public string? Icon { get; set; }
        public string? DefaultRoute { get; set; }
        public string? ComponentKey { get; set; }
        public string? ThemeKey { get; set; }
        public int OrderNo { get; set; }

        public bool IsDefault { get; set; }
        public bool IsLicensed { get; set; }
        public bool IsSystemModule { get; set; }
        public bool ShowInSidebar { get; set; }
        public bool ShowInTopbar { get; set; }

        public string? Category { get; set; }
        public string? Permission { get; set; }

        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }
        public bool IsDeleted { get; set; }

        public string? Description { get; set; }
    }
    public class ModuleCreateDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        [Required, MaxLength(100), RegularExpression("^[A-Za-z0-9_-]+$")]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(100), RegularExpression("^[a-z0-9_-]+$")]
        public string ModuleKey { get; set; } = string.Empty;
        [Required, MaxLength(50), RegularExpression("^(Standard|Optional|AddOn)$")]
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
        [Range(0, int.MaxValue)]
        public int OrderNo { get; set; }
        public bool IsLicensed { get; set; }
        public bool IsSystemModule { get; set; }
        public bool ShowInSidebar { get; set; } = true;
        public bool ShowInTopbar { get; set; }
        public Guid? ParentId { get; set; }
        [MaxLength(100)]
        public string? Permission { get; set; }
        [Required, MaxLength(100)]
        public string? Category { get; set; }
        [MaxLength(500)]
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
    }

    public class ModuleUpdateDto : ModuleCreateDto
    {
        public Guid Id { get; set; }
    }
}
