using Microsoft.AspNetCore.Authorization;

namespace ERP.WorkflowwServices.API.Core
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var userPermissions = context.User
            .Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToList();

            var requiredPermission = context.Resource as string;

            if (requiredPermission != null && userPermissions.Contains(requiredPermission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
