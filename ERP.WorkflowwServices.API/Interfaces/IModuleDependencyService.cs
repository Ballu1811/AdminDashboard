using ERP.WorkflowwServices.API.DTOs;

namespace ERP.WorkflowwServices.API.Interfaces;

public interface IModuleDependencyService
{
    Task<IReadOnlyList<ModuleDependencyDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModuleDependencyDto>> GetByModuleAsync(Guid moduleId, CancellationToken cancellationToken = default);
    Task ReplaceAsync(Guid moduleId, IReadOnlyCollection<Guid> requiredModuleIds, CancellationToken cancellationToken = default);
}
