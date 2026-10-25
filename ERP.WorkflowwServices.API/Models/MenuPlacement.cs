using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WorkflowwServices.API.Interfaces.Common;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblMenuPlacement")]
    public class MenuPlacement : BaseAuditableEntity, ITenantEntity
    {
        [Key]
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? ModuleId { get; set; }
        public Guid MenuId { get; set; }
        public Guid? ParentMenuId { get; set; }
        public int OrderNo { get; set; }

        [ForeignKey(nameof(ModuleId))]
        public Module? Module { get; set; }
        [ForeignKey(nameof(MenuId))]
        public MenuItem? Menu { get; set; }
        [ForeignKey(nameof(ParentMenuId))]
        public MenuItem? ParentMenu { get; set; }
    }
}
