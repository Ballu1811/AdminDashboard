using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Repositories;
using System.Text.Json;

namespace ERP.WorkflowwServices.API.Interfaces;

public interface IWorkflowConfigurationService
{
    Task<PagedResult<T>> ListAsync<T>(FilterModel filter, Guid? workflowId = null, CancellationToken cancellationToken = default) where T : BaseAuditableEntity;
    Task<T?> GetAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseAuditableEntity;
    Task<T> SaveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseAuditableEntity;
    Task<bool> ArchiveAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseAuditableEntity;
    Task<bool> RestoreAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseAuditableEntity;
    Task<WorkflowConditionTestResult> EvaluateConditionAsync(Guid conditionId, JsonElement input, CancellationToken cancellationToken = default);
    Task<List<WorkflowAction>> GetStageActionsAsync(Guid stageId, CancellationToken cancellationToken = default);
    Task SetStageActionsAsync(Guid stageId, IReadOnlyCollection<Guid> actionIds, CancellationToken cancellationToken = default);
}

public sealed record WorkflowConditionTestResult(bool Result, List<string> MatchedRules, List<string> FailedRules, List<string> ValidationErrors);
