using Microsoft.AspNetCore.Authorization;

namespace ERP.WorkflowwServices.API.Core;

public sealed record UserManagementActionRequirement(string Action) : IAuthorizationRequirement;
