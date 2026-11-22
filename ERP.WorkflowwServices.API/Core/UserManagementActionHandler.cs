using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ERP.WorkflowwServices.API.Core;

public sealed class UserManagementActionHandler : AuthorizationHandler<UserManagementActionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserManagementActionRequirement requirement)
    {
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var roles = context.User.Claims
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value.Replace(" ", string.Empty, StringComparison.Ordinal))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var permissionScope = roles.Contains("CompanyAdmin")
            ? "Company"
            : roles.Contains("TenantAdmin") || roles.Contains("TenantOwner")
                ? "Tenant"
                : null;
        var permission = permissionScope is null
            ? null
            : $"UserManagement.{permissionScope}.{requirement.Action}";
        if (permission != null && context.User.HasClaim("permission", permission))
            context.Succeed(requirement);

        if (requirement.Action == "Status")
        {
            if (permissionScope is not null
                && (context.User.HasClaim("permission", $"UserManagement.{permissionScope}.Activate")
                    || context.User.HasClaim("permission", $"UserManagement.{permissionScope}.Deactivate")))
                context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
