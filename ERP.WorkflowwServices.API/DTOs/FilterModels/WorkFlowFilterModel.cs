namespace ERP.WorkflowwServices.API.DTOs.FilterModels
{
    public class WorkFlowFilterModel : BasePaging
    {
        public uint? Status { get; set; }
        public string? KeyWord { get; set; }
        public bool ExportExcel { get; set; } = false;
        public uint? CompanyId { get; set; }
        public int? PageId { get; set; }

        public bool HaveFilter =>
        (!string.IsNullOrWhiteSpace(KeyWord) && KeyWord != "null")
        || Status.HasValue
        || CompanyId.HasValue
        || PageId.HasValue;
    }
}
