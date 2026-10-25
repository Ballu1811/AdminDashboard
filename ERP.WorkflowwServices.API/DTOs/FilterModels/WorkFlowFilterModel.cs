namespace ERP.WorkflowwServices.API.DTOs.FilterModels
{
    public class GridColumnFilter
    {
        public System.Text.Json.JsonElement Value { get; set; }
        public string? MatchMode { get; set; }
        public string? Operator { get; set; }
    }

    public class FilterModel : BasePaging
    {
        public uint? Status { get; set; }
        public string? KeyWord { get; set; }
        public bool ExportExcel { get; set; } = false;
        public uint? CompanyId { get; set; }
        public int? PageId { get; set; }
        public Guid? ParentId { get; set; }
        public Guid? ModuleId { get; set; }
        public string? SortField { get; set; }
        public string? SortOrder { get; set; }
        public Dictionary<string, List<GridColumnFilter>>? Filters { get; set; }

        public bool HaveFilter =>
        (!string.IsNullOrWhiteSpace(KeyWord) && KeyWord != "null")
        || Status.HasValue
        || CompanyId.HasValue
        || PageId.HasValue
        || ModuleId.HasValue
        || ParentId.HasValue;
    }
}
