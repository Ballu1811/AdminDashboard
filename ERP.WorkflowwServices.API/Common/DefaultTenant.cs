using System.Security.Claims;

namespace ERP.WorkflowwServices.API.Common
{
    public static class SystemDefaults
    {
        public static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public static readonly Guid SystemUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    }
}
