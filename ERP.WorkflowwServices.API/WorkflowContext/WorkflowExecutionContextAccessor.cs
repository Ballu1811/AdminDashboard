using ERP.WorkflowwServices.API.Common;

namespace ERP.WorkflowwServices.API.WorkflowContext
{
    public class WorkflowExecutionContextAccessor:IWorkflowExecutionContextAccessor
    {
        private WorkflowExecutionContext _context;
        public WorkflowExecutionContext Context {
            get
            {
                if (_context == null)
                {
                    _context = new WorkflowExecutionContext
                    {
                        TenantId = DefaultTenant.Id,
                        IsSystemAction = true
                    };
                }
                return _context;
            }
            set => _context = value;
        }
    }
}
