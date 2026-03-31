using ERP.WorkflowwServices.API.Interfaces.Common;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblMenuRole")]
    [Index(nameof(MenuId), nameof(RoleId), IsUnique = true)]
    public class MenuRole : BaseAuditableEntity, ITenantEntity
    {
        [Key]
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid MenuId { get; set; }
        public Guid RoleId { get; set; }
       
        public bool CanView { get; set; } = true;
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(MenuId))]
        public MenuItem? Menu { get; set; }

        [ForeignKey(nameof(RoleId))]
        public Roles? Role { get; set; }
    }
}
