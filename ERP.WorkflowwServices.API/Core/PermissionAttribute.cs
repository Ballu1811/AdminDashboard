using Microsoft.AspNetCore.Authorization;

namespace ERP.WorkflowwServices.API.Core
{
    public class PermissionAttribute: AuthorizeAttribute
    {
        public PermissionAttribute(string permission)
        {
            Policy = $"Permission:{permission}";
        }
    }
}
