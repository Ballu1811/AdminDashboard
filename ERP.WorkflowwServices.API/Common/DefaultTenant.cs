namespace ERP.WorkflowwServices.API.Common
{
    public static class DefaultTenant
    {
        // Development tenant
        public static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    }

    public static class SystemUser
    {
        public static readonly Guid Id = Guid.Parse("99999999-9999-9999-9999-999999999999");
    }
}
