namespace ERP.WorkflowwServices.API.DTOs
{
    public class WFEventResponseDto
    {
        public int EventId { get; set; }

        public string? Description { get; set; }

        public string? TableName { get; set; }

        public int? PageId { get; set; }

        public int? CompanyId { get; set; }

        public bool MultiUse { get; set; }

        public string? CheckShow { get; set; }

        public DateTime RecDate { get; set; }
    }
}
