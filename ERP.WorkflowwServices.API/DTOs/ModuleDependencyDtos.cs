using System.ComponentModel.DataAnnotations;

namespace ERP.WorkflowwServices.API.DTOs;

public sealed class ModuleDependencyDto
{
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public Guid RequiredModuleId { get; set; }
    public string RequiredModuleName { get; set; } = string.Empty;
}

public sealed class SaveModuleDependenciesDto
{
    [Required]
    public List<Guid> RequiredModuleIds { get; set; } = [];
}
