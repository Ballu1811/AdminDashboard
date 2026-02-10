using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblWFGroup")]
    public class WFGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GroupId { get; set; }
        [MaxLength(200)]
        public required string GroupName { get; set; }
        public int EventId { get; set; }
        public int? MaxDaysComplete { get; set; }
        public int? CompanyId { get; set; }
        public DateTime RecDate { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public int CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        [Timestamp]
        public byte[]? RowVersion { get; set; }
        public virtual WFEvent? WFEvent { get; set; }
    }
}
