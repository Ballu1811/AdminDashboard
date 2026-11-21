using ERP.WorkflowwServices.API.Interfaces.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models;

[Table("tblModuleDependencies")]
public sealed class ModuleDependency : ITenantEntity
{
    [Key]
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public Guid RequiredModuleId { get; set; }
    public Module RequiredModule { get; set; } = null!;
}
