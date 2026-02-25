using ERP.WorkflowwServices.API.WorkflowContext;

namespace ERP.WorkflowwServices.API.Configurations
{
    public class WorkflowContextMiddleware
    {
        private readonly RequestDelegate _next;
        public WorkflowContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IWorkflowExecutionContextAccessor accessor)
        {
            var tenantId = context.User.FindFirst("tenantId")?.Value;
            accessor.Context = new WorkflowExecutionContext
            {
                TenantId = tenantId != null ? Guid.Parse(tenantId) : Guid.Empty,

                ActorId = context.User.FindFirst("userId") != null
                ? Guid.Parse(context.User.FindFirst("userId")!.Value)
                : null
            };

            await _next(context);
        }
    }
}
