using ERP.WorkflowwServices.API.Interfaces.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblUserRoles")]
    public class UserRole : ITenantEntity
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid TenantId { get; set; }

        [ForeignKey(nameof(UserId))]
        public Users? User { get; set; }

        [ForeignKey(nameof(RoleId))]
        public Roles? Role { get; set; }
    }
}
