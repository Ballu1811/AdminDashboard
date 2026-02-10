using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblWFEvent")]
    public class WFEvent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EventId { get; set; }       
        [MaxLength(250)]
        public required string Description { get; set; }

        [MaxLength(150)]
        public string? TableName { get; set; }
        public int? PageId { get; set; }
        public int? CompanyId { get; set; }   
        public bool MultiUse { get; set; } = false;

        [MaxLength(50)]
        public string? CheckShow { get; set; }

        public DateTime RecDate { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        public int CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
