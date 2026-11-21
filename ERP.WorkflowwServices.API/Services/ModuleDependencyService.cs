using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services;

public sealed class ModuleDependencyService(WorkflowDbContext db) : IModuleDependencyService
{
    public async Task<IReadOnlyList<ModuleDependencyDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Query().OrderBy(x => x.ModuleName).ThenBy(x => x.RequiredModuleName).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ModuleDependencyDto>> GetByModuleAsync(Guid moduleId, CancellationToken cancellationToken = default) =>
        await Query().Where(x => x.ModuleId == moduleId).OrderBy(x => x.RequiredModuleName).ToListAsync(cancellationToken);

    public async Task ReplaceAsync(Guid moduleId, IReadOnlyCollection<Guid> requiredModuleIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = requiredModuleIds.Distinct().ToArray();
        if (distinctIds.Contains(moduleId))
            throw new InvalidOperationException("A module cannot require itself.");

        var activeModuleIds = await db.Modules.AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var activeSet = activeModuleIds.ToHashSet();
        if (!activeSet.Contains(moduleId))
            throw new KeyNotFoundException("Module was not found or is inactive.");
        if (distinctIds.Any(id => !activeSet.Contains(id)))
            throw new InvalidOperationException("Every required module must exist and be active.");

        var edges = await db.ModuleDependencies.AsNoTracking()
            .Where(x => x.ModuleId != moduleId)
            .Select(x => new { x.ModuleId, x.RequiredModuleId })
            .ToListAsync(cancellationToken);
        var graph = edges.GroupBy(x => x.ModuleId)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.RequiredModuleId).ToList());
        foreach (var requiredId in distinctIds)
        {
            var pending = new Stack<Guid>();
            var visited = new HashSet<Guid>();
            pending.Push(requiredId);
            while (pending.TryPop(out var current))
            {
                if (current == moduleId)
                    throw new InvalidOperationException("These dependencies would create a circular module requirement.");
                if (!visited.Add(current) || !graph.TryGetValue(current, out var next)) continue;
                foreach (var nextId in next) pending.Push(nextId);
            }
        }

        var existing = await db.ModuleDependencies.Where(x => x.ModuleId == moduleId).ToListAsync(cancellationToken);
        db.ModuleDependencies.RemoveRange(existing);
        foreach (var requiredId in distinctIds)
        {
            db.ModuleDependencies.Add(new ModuleDependency
            {
                Id = Guid.NewGuid(),
                ModuleId = moduleId,
                RequiredModuleId = requiredId
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ModuleDependencyDto> Query() => db.ModuleDependencies.AsNoTracking()
        .Select(x => new ModuleDependencyDto
        {
            ModuleId = x.ModuleId,
            ModuleName = x.Module.Name,
            RequiredModuleId = x.RequiredModuleId,
            RequiredModuleName = x.RequiredModule.Name
        });
}
