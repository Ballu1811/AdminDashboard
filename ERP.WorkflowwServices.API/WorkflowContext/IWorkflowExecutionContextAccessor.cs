namespace ERP.WorkflowwServices.API.WorkflowContext
{
    public interface IWorkflowExecutionContextAccessor
    {
        WorkflowExecutionContext Context { get; set; }
    }
}
