using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Services;
using Microsoft.AspNetCore.Hosting;

namespace ERP.WorkflowwServices.API.Configurations
{
    public static class ApplicationServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IWebHostEnvironment environment)
        {
            services.AddScoped<IWFEvent, WFEventService>();

            return services;
        }
    }
}
