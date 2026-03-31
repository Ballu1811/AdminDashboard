namespace ERP.WorkflowwServices.API.DTOs
{
    public class ModuleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Icon { get; set; }
        public string? DefaultRoute { get; set; }
        public int OrderNo { get; set; }

        public bool IsDefault { get; set; }
        public bool IsLicensed { get; set; }

        public string? Category { get; set; }
        public string? Permission { get; set; }

        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }

        public string? Description { get; set; }
    }
    public class ModuleCreateDto
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Icon { get; set; }
        public string? DefaultRoute { get; set; }
        public int OrderNo { get; set; }
        public bool IsLicensed { get; set; }
        public string? Permission { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
    }

    public class ModuleUpdateDto : ModuleCreateDto
    {
        public Guid Id { get; set; }
    }
}
