using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.WorkflowContext;
using System.Security.Claims;

namespace ERP.WorkflowwServices.API.Configurations
{
    public class WorkflowContextMiddleware
    {
        private readonly RequestDelegate _next;
        public WorkflowContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IUserContext userContext, IWorkflowExecutionContextAccessor workflow)
        {
            workflow.Context = new WorkflowExecutionContext
            {
                TenantId = userContext.TenantId,
                ActorId = userContext.IsAuthenticated ? userContext.UserId : SystemDefaults.SystemUserId,
                IsSystemAction = !userContext.IsAuthenticated
            };

            await _next(context);
        }
    }
}
