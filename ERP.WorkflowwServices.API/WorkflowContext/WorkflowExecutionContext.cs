namespace ERP.WorkflowwServices.API.WorkflowContext
{
    public class WorkflowExecutionContext
    {
        public Guid TenantId { get; set; }
        public Guid? ActorId { get; set; }
        public Guid WorkflowInstanceId { get; set; }
        public bool IsSystemAction { get; set; }
    }
}
