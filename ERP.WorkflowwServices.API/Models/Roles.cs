using ERP.WorkflowwServices.API.Interfaces.Common;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblRoles")]
    [Index(nameof(Name), nameof(TenantId), IsUnique = true)]
    public class Roles : BaseAuditableEntity, ITenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        // 🔥 Multi-tenant support
        public Guid TenantId { get; set; }

        // ================= BASIC =================
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Code { get; set; }   // ADMIN, HR_MANAGER

        [MaxLength(250)]
        public string? Description { get; set; }
        // ================= HIERARCHY =================
        public Guid? ParentRoleId { get; set; }

        [ForeignKey(nameof(ParentRoleId))]
        public Roles? ParentRole { get; set; }

        public ICollection<Roles> Children { get; set; } = new List<Roles>();

        // ================= UI =================
        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(50)]
        public string? Badge { get; set; }

        public string? NormalizedName { get; set; }   // fast search
        public int Priority { get; set; }             // role priority
        public bool IsEditable { get; set; } = true;  // lock role

        // ================= FLAGS =================
        public bool IsActive { get; set; } = true;

        public bool IsSystem { get; set; } = false;   // system roles (cannot delete)

        public bool IsDefault { get; set; } = false;  // auto assign to new users

        // ================= NAVIGATION =================
        public bool ShowInUI { get; set; } = true;

        // ================= RELATIONS =================
        public ICollection<MenuRole> MenuRoles { get; set; } = new List<MenuRole>();

        public ICollection<Users> Users { get; set; } = new List<Users>();
    }
}
