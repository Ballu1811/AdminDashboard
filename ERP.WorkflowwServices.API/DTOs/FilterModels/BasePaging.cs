namespace ERP.WorkflowwServices.API.DTOs.FilterModels
{
    public class BasePaging
    {
        private const int MaxPageSize = 1000;
        public int PageNumber { get; set; } = 1;
        private int _pageSize = 20;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        /* Sorting support */
        public string? SortBy { get; set; }        // CreatedDate, Status
        public string? SortDirection { get; set; } = "desc";  // asc / desc

        /* Date filtering (very useful in ERP) */
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
