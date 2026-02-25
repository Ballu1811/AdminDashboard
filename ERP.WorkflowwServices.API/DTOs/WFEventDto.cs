using System.ComponentModel.DataAnnotations;

namespace ERP.WorkflowwServices.API.DTOs
{
    public class WFEventDto
    {
        public int? EventId { get; set; }
        [Required]
        [MaxLength(100)]
        public string EventKey { get; set; } = string.Empty;
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;
        [MaxLength(150)]
        public string? SourceEntity { get; set; }   
        public Guid TenantId { get; set; }
        public bool MultiUse { get; set; }
        [MaxLength(50)]
        public string? CheckShow { get; set; }
        public bool IsActive { get; set; } = true;
        public byte[]? RowVersion { get; set; }
    }
}
