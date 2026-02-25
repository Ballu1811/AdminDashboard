namespace ERP.WorkflowwServices.API.WorkflowContext
{
    public class WorkflowExecutionContextAccessor:IWorkflowExecutionContextAccessor
    {
        public WorkflowExecutionContext Context { get; set; }  = new WorkflowExecutionContext();
    }
}
