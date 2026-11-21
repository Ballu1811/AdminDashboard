using System.Text.Json;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.WorkflowwServices.API.Controllers;

[ApiController]
[Route("api/workflow-configuration")]
[Authorize(Roles = "SuperAdmin")]
public sealed class WorkflowConfigurationController(IWorkflowConfigurationService service) : ControllerBase
{
    private static readonly Dictionary<string, Type> ResourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["workflows"] = typeof(GlobalWorkflow), ["stages"] = typeof(WorkflowStage),
        ["actions"] = typeof(WorkflowAction), ["stage-actions"] = typeof(WorkflowStageAction),
        ["conditions"] = typeof(WorkflowCondition), ["transitions"] = typeof(WorkflowTransition)
    };

    [HttpGet("{resource}")]
    public async Task<IActionResult> List(string resource, [FromQuery] FilterModel filter, [FromQuery] Guid? workflowId, CancellationToken cancellationToken)
    {
        if (!ResourceTypes.TryGetValue(resource, out var type)) return NotFound();
        return Ok(await InvokeList(type, filter, workflowId, cancellationToken));
    }

    [HttpGet("{resource}/{id:guid}")]
    public async Task<IActionResult> Get(string resource, Guid id, CancellationToken cancellationToken)
    {
        if (!ResourceTypes.TryGetValue(resource, out var type)) return NotFound();
        var result = await InvokeGet(type, id, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("{resource}")]
    public async Task<IActionResult> Create(string resource, [FromBody] JsonElement body, CancellationToken cancellationToken) => await Save(resource, body, cancellationToken);

    [HttpPut("{resource}/{id:guid}")]
    public async Task<IActionResult> Update(string resource, Guid id, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        if (!body.TryGetProperty("id", out var jsonId) || jsonId.GetGuid() != id) return BadRequest(new { message = "Id mismatch." });
        return await Save(resource, body, cancellationToken);
    }

    [HttpPatch("{resource}/{id:guid}/archive")]
    public async Task<IActionResult> Archive(string resource, Guid id, CancellationToken cancellationToken) => await SetArchived(resource, id, true, cancellationToken);

    [HttpPatch("{resource}/{id:guid}/restore")]
    public async Task<IActionResult> Restore(string resource, Guid id, CancellationToken cancellationToken) => await SetArchived(resource, id, false, cancellationToken);

    [HttpPost("conditions/{id:guid}/test")]
    public async Task<IActionResult> TestCondition(Guid id, [FromBody] JsonElement sample, CancellationToken cancellationToken)
    {
        var result = await service.EvaluateConditionAsync(id, sample, cancellationToken);
        return Ok(result);
    }

    [HttpGet("stages/{id:guid}/actions")]
    public async Task<IActionResult> GetStageActions(Guid id, CancellationToken cancellationToken) => Ok(await service.GetStageActionsAsync(id, cancellationToken));

    [HttpPut("stages/{id:guid}/actions")]
    public async Task<IActionResult> SetStageActions(Guid id, [FromBody] List<Guid> actionIds, CancellationToken cancellationToken)
    {
        try { await service.SetStageActionsAsync(id, actionIds ?? [], cancellationToken); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    private async Task<IActionResult> Save(string resource, JsonElement body, CancellationToken cancellationToken)
    {
        if (!ResourceTypes.TryGetValue(resource, out var type)) return NotFound();
        try
        {
            var entity = JsonSerializer.Deserialize(body, type, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (entity is not BaseAuditableEntity record) return BadRequest(new { message = "Invalid workflow configuration payload." });
            return Ok(await InvokeSave(type, record, cancellationToken));
        }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException) { return Conflict(new { message = "A duplicate code or configuration constraint already exists." }); }
        catch (JsonException) { return BadRequest(new { message = "Invalid request payload." }); }
    }

    private async Task<IActionResult> SetArchived(string resource, Guid id, bool archive, CancellationToken cancellationToken)
    {
        if (!ResourceTypes.TryGetValue(resource, out var type)) return NotFound();
        var result = await InvokeArchive(type, id, archive, cancellationToken);
        return result ? NoContent() : NotFound();
    }

    private Task<object> InvokeList(Type type, FilterModel filter, Guid? workflowId, CancellationToken ct) => (Task<object>)GetType()
        .GetMethod(nameof(ListGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
        .MakeGenericMethod(type).Invoke(this, [filter, workflowId, ct])!;
    private async Task<object> ListGeneric<T>(FilterModel filter, Guid? workflowId, CancellationToken ct) where T : BaseAuditableEntity => await service.ListAsync<T>(filter, workflowId, ct);
    private Task<object?> InvokeGet(Type type, Guid id, CancellationToken ct) => (Task<object?>)GetType()
        .GetMethod(nameof(GetGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
        .MakeGenericMethod(type).Invoke(this, [id, ct])!;
    private async Task<object?> GetGeneric<T>(Guid id, CancellationToken ct) where T : BaseAuditableEntity => await service.GetAsync<T>(id, ct);
    private Task<object> InvokeSave(Type type, BaseAuditableEntity entity, CancellationToken ct) => (Task<object>)GetType()
        .GetMethod(nameof(SaveGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
        .MakeGenericMethod(type).Invoke(this, [entity, ct])!;
    private async Task<object> SaveGeneric<T>(BaseAuditableEntity entity, CancellationToken ct) where T : BaseAuditableEntity => await service.SaveAsync((T)entity, ct);
    private Task<bool> InvokeArchive(Type type, Guid id, bool archive, CancellationToken ct) => (Task<bool>)GetType()
        .GetMethod(nameof(ArchiveGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
        .MakeGenericMethod(type).Invoke(this, [id, archive, ct])!;
    private async Task<bool> ArchiveGeneric<T>(Guid id, bool archive, CancellationToken ct) where T : BaseAuditableEntity => archive
        ? await service.ArchiveAsync<T>(id, ct) : await service.RestoreAsync<T>(id, ct);
}
