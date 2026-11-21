using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ERP.WorkflowwServices.API.Services;

public sealed class WorkflowConfigurationService(WorkflowDbContext db, IUserContext user) : IWorkflowConfigurationService
{
    private static readonly HashSet<string> AllowedConditionFields = new(StringComparer.OrdinalIgnoreCase)
        { "amount", "quantity", "status", "department", "requesterId", "createdAt", "currency" };

    public async Task<WorkflowConditionTestResult> EvaluateConditionAsync(Guid conditionId, JsonElement input, CancellationToken cancellationToken = default)
    {
        if (input.ValueKind != JsonValueKind.Object) return new(false, [], [], ["Sample input must be a JSON object."]);
        var condition = await db.WorkflowConditions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == conditionId && x.IsActive, cancellationToken);
        if (condition == null) return new(false, [], [], ["Condition was not found or is inactive."]);
        if (!IsValidRuleTree(condition.RulesJson)) return new(false, [], [], ["Stored rule configuration is invalid."]);
        using var rulesDoc = JsonDocument.Parse(condition.RulesJson);
        var matched = new List<string>();
        var failed = new List<string>();
        var errors = new List<string>();
        var result = EvaluateNode(rulesDoc.RootElement, input, matched, failed, errors);
        return new(result, matched, failed, errors);
    }

    public async Task<List<WorkflowAction>> GetStageActionsAsync(Guid stageId, CancellationToken cancellationToken = default)
    {
        return await db.WorkflowStageActions.AsNoTracking().Where(x => x.WorkflowStageId == stageId && x.IsActive)
            .Join(db.WorkflowActions, mapping => mapping.WorkflowActionId, action => action.Id, (_, action) => action)
            .OrderBy(action => action.DisplayOrder).ToListAsync(cancellationToken);
    }

    public async Task SetStageActionsAsync(Guid stageId, IReadOnlyCollection<Guid> actionIds, CancellationToken cancellationToken = default)
    {
        if (!await db.WorkflowStages.AnyAsync(stage => stage.Id == stageId, cancellationToken)) throw new KeyNotFoundException("Workflow stage not found.");
        var uniqueIds = actionIds.Distinct().ToArray();
        if (await db.WorkflowActions.CountAsync(action => uniqueIds.Contains(action.Id) && action.IsActive, cancellationToken) != uniqueIds.Length)
            throw new InvalidOperationException("One or more actions are unavailable.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var existing = await db.WorkflowStageActions.Where(mapping => mapping.WorkflowStageId == stageId).ToListAsync(cancellationToken);
        var selected = uniqueIds.ToHashSet();
        foreach (var mapping in existing)
        {
            mapping.IsActive = selected.Contains(mapping.WorkflowActionId);
            selected.Remove(mapping.WorkflowActionId);
        }
        foreach (var actionId in selected)
            db.WorkflowStageActions.Add(new WorkflowStageAction { Id = Guid.NewGuid(), TenantId = user.TenantId, WorkflowStageId = stageId, WorkflowActionId = actionId });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<PagedResult<T>> ListAsync<T>(FilterModel filter, Guid? workflowId = null, CancellationToken cancellationToken = default) where T : BaseAuditableEntity
    {
        var query = db.Set<T>().IgnoreQueryFilters().AsNoTracking();
        if (typeof(ERP.WorkflowwServices.API.Interfaces.Common.ITenantEntity).IsAssignableFrom(typeof(T)))
            query = query.Where(x => EF.Property<Guid>(x, "TenantId") == user.TenantId);
        if (workflowId.HasValue && typeof(T) != typeof(GlobalWorkflow) && typeof(T) != typeof(WorkflowAction))
        {
            if (typeof(T) == typeof(WorkflowStageAction))
            {
                var stageIds = db.WorkflowStages.IgnoreQueryFilters().Where(s => s.TenantId == user.TenantId && s.WorkflowId == workflowId.Value).Select(s => s.Id);
                query = query.Where(x => stageIds.Contains(EF.Property<Guid>(x, "WorkflowStageId")));
            }
            else query = query.Where(x => EF.Property<Guid>(x, "WorkflowId") == workflowId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.KeyWord))
        {
            var keyword = filter.KeyWord.Trim();
            if (typeof(T) == typeof(GlobalWorkflow)) query = query.Where(x => EF.Property<string>(x, "Name").Contains(keyword) || EF.Property<string>(x, "Code").Contains(keyword));
            else if (typeof(T) == typeof(WorkflowAction)) query = query.Where(x => EF.Property<string>(x, "Name").Contains(keyword) || EF.Property<string>(x, "Code").Contains(keyword));
            else query = query.Where(x => EF.Property<string>(x, "Name").Contains(keyword) || EF.Property<string>(x, "Code").Contains(keyword));
        }
        if (filter.Status == 0) query = query.Where(x => !EF.Property<bool>(x, nameof(BaseAuditableEntity.IsDeleted)));
        else if (filter.Status == 1) query = query.Where(x => EF.Property<bool>(x, nameof(BaseAuditableEntity.IsDeleted)));
        else if (filter.Status == 2) query = query.Where(x => !EF.Property<bool>(x, nameof(BaseAuditableEntity.IsDeleted)) && !EF.Property<bool>(x, "IsActive"));

        var total = await query.CountAsync(cancellationToken);
        var page = Math.Max(filter.PageNumber, 1);
        var size = Math.Clamp(filter.PageSize, 1, 100);
        var rows = await query.OrderBy(x => EF.Property<DateTime>(x, nameof(BaseAuditableEntity.CreatedAt)))
            .Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new PagedResult<T> { Total = total, Data = rows };
    }

    public Task<T?> GetAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseAuditableEntity =>
        db.Set<T>().AsNoTracking().FirstOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == id, cancellationToken);

    public async Task<T> SaveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseAuditableEntity
    {
        Validate(entity);
        var id = (Guid)(typeof(T).GetProperty("Id")?.GetValue(entity) ?? Guid.Empty);
        var current = id == Guid.Empty ? null : await db.Set<T>().FirstOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == id, cancellationToken);
        if (current == null)
        {
            typeof(T).GetProperty("Id")?.SetValue(entity, Guid.NewGuid());
            db.Set<T>().Add(entity);
        }
        else
        {
            db.Entry(current).CurrentValues.SetValues(entity);
            entity = current;
        }
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> ArchiveAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseAuditableEntity
    {
        var entity = await db.Set<T>().FirstOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == id, cancellationToken);
        if (entity == null) return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = user.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RestoreAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseAuditableEntity
    {
        var entity = await db.Set<T>().IgnoreQueryFilters().FirstOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == id && EF.Property<Guid>(x, "TenantId") == user.TenantId, cancellationToken);
        if (entity == null) return false;
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private void Validate<T>(T entity) where T : BaseAuditableEntity
    {
        var tenant = user.TenantId;
        if (entity is GlobalWorkflow workflow)
        {
            if (string.IsNullOrWhiteSpace(workflow.Code) || string.IsNullOrWhiteSpace(workflow.Name)) throw new InvalidOperationException("Workflow code and name are required.");
            if (workflow.MaximumApprovalLevels < 1 || workflow.DefaultSlaHours < 0) throw new InvalidOperationException("Workflow approval levels and SLA values must be positive.");
            if (!new[] { "Sequential", "Conditional", "Parallel", "Hybrid" }.Contains(workflow.WorkflowType)) throw new InvalidOperationException("Workflow type is not supported.");
            workflow.Code = workflow.Code.Trim().ToUpperInvariant();
            workflow.Name = workflow.Name.Trim();
        }
        if (entity is WorkflowStage stage)
        {
            if (stage.WorkflowId == Guid.Empty || !db.GlobalWorkflows.Any(w => w.Id == stage.WorkflowId)) throw new InvalidOperationException("Select a valid workflow.");
            if (stage.SequenceNumber < 1) throw new InvalidOperationException("Sequence number must be positive.");
            var stageId = stage.Id;
            if (stage.StageType is "Start" or "End" && db.WorkflowStages.Any(existing => existing.WorkflowId == stage.WorkflowId && existing.StageType == stage.StageType && existing.Id != stageId))
                throw new InvalidOperationException($"A workflow can only have one {stage.StageType} stage.");
            if (stage.AssignmentType == "RoleBased" && stage.RoleId == null && stage.StageType == "Approval") throw new InvalidOperationException("Select a role for role based approval stages.");
            stage.Code = stage.Code.Trim().ToUpperInvariant();
        }
        if (entity is WorkflowCondition condition)
        {
            if (!db.GlobalWorkflows.Any(w => w.Id == condition.WorkflowId)) throw new InvalidOperationException("Select a valid workflow.");
            if (!IsValidRuleTree(condition.RulesJson)) throw new InvalidOperationException("Condition rule tree is invalid or uses unsupported fields/operators.");
            condition.Code = condition.Code.Trim().ToUpperInvariant();
        }
        if (entity is WorkflowStageAction stageAction)
        {
            var stageRecord = db.WorkflowStages.FirstOrDefault(x => x.Id == stageAction.WorkflowStageId);
            if (stageRecord == null || !db.WorkflowActions.Any(x => x.Id == stageAction.WorkflowActionId && x.IsActive)) throw new InvalidOperationException("Select an active stage and action.");
        }
        if (entity is WorkflowTransition transition)
        {
            var validStages = db.WorkflowStages.Where(s => s.WorkflowId == transition.WorkflowId).Select(s => s.Id);
            if (!validStages.Contains(transition.FromStageId) || !validStages.Contains(transition.ToStageId)) throw new InvalidOperationException("Both transition stages must belong to the selected workflow.");
            if (transition.FromStageId == transition.ToStageId && transition.TransitionType != "Exception") throw new InvalidOperationException("Self transitions are only allowed for exception transitions.");
            if (transition.ConditionId.HasValue && !db.WorkflowConditions.Any(c => c.Id == transition.ConditionId && c.WorkflowId == transition.WorkflowId)) throw new InvalidOperationException("Condition must belong to the selected workflow.");
            if (!db.WorkflowActions.Any(a => a.Id == transition.WorkflowActionId)) throw new InvalidOperationException("Select a valid action.");
            if (transition.IsDefault && db.WorkflowTransitions.Any(t => t.WorkflowId == transition.WorkflowId && t.FromStageId == transition.FromStageId && t.WorkflowActionId == transition.WorkflowActionId && t.IsDefault && t.Id != transition.Id))
                throw new InvalidOperationException("Only one default transition is allowed for a stage and action.");
            transition.Code = transition.Code.Trim().ToUpperInvariant();
        }
        if (typeof(ERP.WorkflowwServices.API.Interfaces.Common.ITenantEntity).IsAssignableFrom(typeof(T))) typeof(T).GetProperty("TenantId")?.SetValue(entity, tenant);
    }

    private static bool IsValidRuleTree(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "amount", "quantity", "status", "department", "requesterId", "createdAt", "currency" };
            var operators = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "equals", "notequals", "greaterthan", "greaterthanorequal", "lessthan", "lessthanorequal", "between", "contains", "startswith", "in", "notin", "istrue", "isfalse", "and", "or" };
            return ValidateNode(doc.RootElement, fields, operators);
        }
        catch { return false; }
    }

    private static bool ValidateNode(System.Text.Json.JsonElement node, HashSet<string> fields, HashSet<string> operators)
    {
        if (node.ValueKind != System.Text.Json.JsonValueKind.Object) return false;
        if (node.TryGetProperty("children", out var children))
            return node.TryGetProperty("operator", out var groupOperator) && new[] { "and", "or" }.Contains(groupOperator.GetString() ?? "", StringComparer.OrdinalIgnoreCase) &&
                children.ValueKind == System.Text.Json.JsonValueKind.Array && children.GetArrayLength() > 0 && children.EnumerateArray().All(child => ValidateNode(child, fields, operators));
        return node.TryGetProperty("field", out var field) && fields.Contains(field.GetString() ?? "") && node.TryGetProperty("operator", out var op) && operators.Contains((op.GetString() ?? "").Replace(" ", ""));
    }

    private static bool EvaluateNode(JsonElement node, JsonElement input, List<string> matched, List<string> failed, List<string> errors)
    {
        if (node.TryGetProperty("children", out var children))
        {
            var results = children.EnumerateArray().Select(child => EvaluateNode(child, input, matched, failed, errors)).ToList();
            return string.Equals(node.GetProperty("operator").GetString(), "or", StringComparison.OrdinalIgnoreCase) ? results.Any(x => x) : results.All(x => x);
        }

        var fieldName = node.GetProperty("field").GetString()!;
        var label = $"{fieldName} {node.GetProperty("operator").GetString()}";
        var actualProperty = input.EnumerateObject().FirstOrDefault(p => string.Equals(p.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        if (actualProperty.Name == null) { errors.Add($"Input is missing whitelisted field '{fieldName}'."); failed.Add(label); return false; }
        var expected = node.TryGetProperty("value", out var value) ? value : default;
        var operation = node.GetProperty("operator").GetString()!.Replace(" ", "").ToLowerInvariant();
        try
        {
            var actual = actualProperty.Value;
            var result = operation switch
            {
                "equals" => JsonEqual(actual, expected), "notequals" => !JsonEqual(actual, expected),
                "greaterthan" => Compare(actual, expected) > 0, "greaterthanorequal" => Compare(actual, expected) >= 0,
                "lessthan" => Compare(actual, expected) < 0, "lessthanorequal" => Compare(actual, expected) <= 0,
                "contains" => actual.ValueKind == JsonValueKind.String && actual.GetString()!.Contains(expected.GetString() ?? "", StringComparison.OrdinalIgnoreCase),
                "startswith" => actual.ValueKind == JsonValueKind.String && actual.GetString()!.StartsWith(expected.GetString() ?? "", StringComparison.OrdinalIgnoreCase),
                "between" => expected.ValueKind == JsonValueKind.Array && expected.GetArrayLength() == 2 && Compare(actual, expected[0]) >= 0 && Compare(actual, expected[1]) <= 0,
                "in" => expected.ValueKind == JsonValueKind.Array && expected.EnumerateArray().Any(item => JsonEqual(actual, item)),
                "notin" => expected.ValueKind == JsonValueKind.Array && expected.EnumerateArray().All(item => !JsonEqual(actual, item)),
                "istrue" => actual.ValueKind == JsonValueKind.True,
                "isfalse" => actual.ValueKind == JsonValueKind.False,
                _ => throw new InvalidOperationException("Unsupported operator.")
            };
            (result ? matched : failed).Add(label);
            return result;
        }
        catch (Exception exception) { errors.Add($"Could not evaluate '{fieldName}': {exception.Message}"); failed.Add(label); return false; }
    }

    private static bool JsonEqual(JsonElement left, JsonElement right) =>
        left.ValueKind == JsonValueKind.Number && right.ValueKind == JsonValueKind.Number
            ? left.GetDecimal() == right.GetDecimal()
            : left.ValueKind == JsonValueKind.String && right.ValueKind == JsonValueKind.String
                ? string.Equals(left.GetString(), right.GetString(), StringComparison.OrdinalIgnoreCase)
                : left.ValueKind == right.ValueKind && left.ToString() == right.ToString();

    private static int Compare(JsonElement left, JsonElement right)
    {
        if (left.ValueKind == JsonValueKind.Number && right.ValueKind == JsonValueKind.Number) return left.GetDecimal().CompareTo(right.GetDecimal());
        if (left.ValueKind == JsonValueKind.String && right.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(left.GetString(), out var leftDate) && DateTime.TryParse(right.GetString(), out var rightDate)) return leftDate.CompareTo(rightDate);
            return string.Compare(left.GetString(), right.GetString(), StringComparison.OrdinalIgnoreCase);
        }
        throw new InvalidOperationException("Values must have matching comparable types.");
    }
}
