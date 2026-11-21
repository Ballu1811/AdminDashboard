using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WorkflowwServices.API.Interfaces.Common;

namespace ERP.WorkflowwServices.API.Models;

[Table("tblDepartments")]
public sealed class Department : BaseAuditableEntity, ITenantEntity
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Code { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> ChildDepartments { get; set; } = new List<Department>();
}
