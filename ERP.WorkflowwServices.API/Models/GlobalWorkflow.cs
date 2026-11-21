using ERP.WorkflowwServices.API.Interfaces.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models;

[Table("tblGlobalWorkflows")]
public sealed class GlobalWorkflow : BaseAuditableEntity, ITenantEntity
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    [MaxLength(150)] public string Name { get; set; } = string.Empty;
    [MaxLength(80)] public string Category { get; set; } = string.Empty;
    [MaxLength(30)] public string WorkflowType { get; set; } = "Sequential";
    [MaxLength(30)] public string Scope { get; set; } = "Global";
    [MaxLength(1000)] public string? Description { get; set; }
    public int Version { get; set; } = 1;
    public bool IsDraft { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool AllowRejection { get; set; } = true;
    public bool AllowResubmission { get; set; } = true;
    public bool AllowCancellation { get; set; }
    public bool AllowSendBack { get; set; }
    public bool EnableDelegation { get; set; }
    public bool EnableEscalation { get; set; }
    public bool EnableSla { get; set; }
    public int? DefaultSlaHours { get; set; }
    public int MaximumApprovalLevels { get; set; } = 10;
    [MaxLength(30)] public string AssignmentStrategy { get; set; } = "RoleBased";
    public Guid? DefaultApproverRoleId { get; set; }
    public bool AllowMultipleApprovers { get; set; }
    [MaxLength(30)] public string ApprovalMode { get; set; } = "AnyOne";
    public bool AllowParallelExecution { get; set; }
    public bool AllowConditionalRouting { get; set; }
    public bool EnableAuditTrail { get; set; } = true;
    public bool AutoCloseOnCompletion { get; set; }
}

[Table("tblWorkflowStages")]
public sealed class WorkflowStage : BaseAuditableEntity, ITenantEntity
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WorkflowId { get; set; }
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    [MaxLength(150)] public string Name { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(30)] public string StageType { get; set; } = "Approval";
    public int SequenceNumber { get; set; }
    [MaxLength(30)] public string AssignmentType { get; set; } = "RoleBased";
    public Guid? RoleId { get; set; }
    public Guid? UserId { get; set; }
    [MaxLength(1000)] public string? DynamicAssignmentRule { get; set; }
    [MaxLength(30)] public string ApprovalMode { get; set; } = "AnyOne";
    public int MinimumApprovals { get; set; } = 1;
    public bool IsMandatory { get; set; } = true;
    public bool AllowApprove { get; set; } = true;
    public bool AllowReject { get; set; } = true;
    public bool AllowSendBack { get; set; }
    public bool AllowReassign { get; set; }
    public bool AllowDelegate { get; set; }
    public bool AllowSkip { get; set; }
    public bool RequireRemarks { get; set; }
    public bool RequireAttachment { get; set; }
    public bool EnableSla { get; set; }
    public int? SlaDuration { get; set; }
    [MaxLength(20)] public string? SlaUnit { get; set; }
    public bool EscalationEnabled { get; set; }
    public int? EscalationAfter { get; set; }
    public Guid? EscalationRoleId { get; set; }
    public bool IsActive { get; set; } = true;
}

[Table("tblWorkflowActions")]
public sealed class WorkflowAction : BaseAuditableEntity, ITenantEntity
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(100)] public string DisplayName { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(30)] public string ActionType { get; set; } = "UserAction";
    [MaxLength(30)] public string Category { get; set; } = "Approval";
    [MaxLength(80)] public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool RequiresRemarks { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool RequiresConfirmation { get; set; }
    public bool AllowOnMobile { get; set; } = true;
    public bool IsTerminal { get; set; }
    public bool IsSystemAction { get; set; }
    public string? AllowedStageTypesJson { get; set; }
    public string? AllowedAssignmentTypesJson { get; set; }
    [MaxLength(100)] public string? PermissionKey { get; set; }
    [MaxLength(40)] public string AvailableFor { get; set; } = "AssignedApprover";
    [MaxLength(100)] public string? ExecutionHandlerKey { get; set; }
    [MaxLength(30)] public string ExecutionTiming { get; set; } = "AfterTransition";
    public bool RetryEnabled { get; set; }
    public int MaximumRetryCount { get; set; }
    public bool IsActive { get; set; } = true;
}

[Table("tblWorkflowStageActions")]
public sealed class WorkflowStageAction : BaseAuditableEntity, ITenantEntity
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WorkflowStageId { get; set; }
    public Guid WorkflowActionId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

[Table("tblWorkflowConditions")]
public sealed class WorkflowCondition : BaseAuditableEntity, ITenantEntity
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WorkflowId { get; set; }
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    [MaxLength(150)] public string Name { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(30)] public string ConditionType { get; set; } = "Simple";
    [MaxLength(10)] public string EvaluationMode { get; set; } = "AND";
    public string RulesJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
}

[Table("tblWorkflowTransitions")]
public sealed class WorkflowTransition : BaseAuditableEntity, ITenantEntity
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid FromStageId { get; set; }
    public Guid ToStageId { get; set; }
    public Guid WorkflowActionId { get; set; }
    public Guid? ConditionId { get; set; }
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    [MaxLength(150)] public string Name { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(30)] public string TransitionType { get; set; } = "Normal";
    [MaxLength(40)] public string ConditionEvaluation { get; set; } = "Always";
    public int Priority { get; set; }
    public bool IsDefault { get; set; }
    public int? ConditionPriority { get; set; }
    public bool RequireRemarks { get; set; }
    public bool RequireAttachment { get; set; }
    public bool NotifyOnTransition { get; set; }
    public bool IsActive { get; set; } = true;
}
