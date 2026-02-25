using ERP.WorkflowwServices.API.Interfaces.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblWFEvent")]
    public class WFEvent: BaseAuditableEntity, ITenantEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EventId { get; set; }
        [MaxLength(100)]
        public required string EventKey { get; set; }
        [MaxLength(150)]
        public required string Name { get; set; }
        [MaxLength(250)]
        public required string Description { get; set; }      
        [MaxLength(150)]
        public string? SourceEntity { get; set; }
        [MaxLength(100)]
        public string? ContextKey { get; set; }
        public Guid TenantId { get; set; }   
        public bool MultiUse { get; set; } = false;
        [MaxLength(50)]
        public string? CheckShow { get; set; }        
        [Timestamp]
        public byte[]? RowVersion { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
