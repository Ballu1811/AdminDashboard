namespace ERP.WorkflowwServices.API.DTOs
{
    public class MenuPlacementDto
    {
        public Guid Id { get; set; }
        public Guid MenuId { get; set; }
        public string MenuLabel { get; set; } = string.Empty;
        public Guid? ModuleId { get; set; }
        public string? ModuleName { get; set; }
        public Guid? ParentMenuId { get; set; }
        public string? ParentMenuLabel { get; set; }
        public int OrderNo { get; set; }
    }

    public class MenuPlacementCreateDto
    {
        public Guid MenuId { get; set; }
        public Guid? ModuleId { get; set; }
        public Guid? ParentMenuId { get; set; }
        public int OrderNo { get; set; }
    }

    public class MenuPlacementUpdateDto : MenuPlacementCreateDto
    {
        public Guid Id { get; set; }
    }
}
