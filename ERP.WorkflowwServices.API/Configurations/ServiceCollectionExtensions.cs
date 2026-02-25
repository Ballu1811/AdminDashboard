using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Repositories.Implementations;
using ERP.WorkflowwServices.API.Repositories.Interfaces;
using ERP.WorkflowwServices.API.Services;
using ERP.WorkflowwServices.API.WorkflowContext;

namespace ERP.WorkflowwServices.API.Configurations
{
    public static class ApplicationServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IWebHostEnvironment environment)
        {
            services.AddScoped<IWFEvent, WFEventService>();
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
            services.AddScoped<IWorkflowExecutionContextAccessor, WorkflowExecutionContextAccessor>();
            services.AddScoped<IMenuService, MenuService>();

            return services;
        }
    }
}
