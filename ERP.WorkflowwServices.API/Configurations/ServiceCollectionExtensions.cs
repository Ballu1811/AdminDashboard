using ERP.WorkflowwServices.API.Core;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Repositories.Implementations;
using ERP.WorkflowwServices.API.Repositories.Interfaces;
using ERP.WorkflowwServices.API.Services;
using ERP.WorkflowwServices.API.WorkflowContext;
using Microsoft.AspNetCore.Authorization;

namespace ERP.WorkflowwServices.API.Configurations
{
    public static class ApplicationServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IWebHostEnvironment environment)
        {
            // ===============================
            // GENERIC REPOSITORY
            // ===============================
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

            // ===============================
            // UNIT OF WORK 🔥
            // ===============================
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ===============================
            // AUTH SERVICES 🔐
            // ===============================
            services.AddScoped<AuthService>();
            services.AddScoped<IJwtService, JwtService>();

            // ===============================
            // WORKFLOW CONTEXT
            // ===============================
            services.AddScoped<IWorkflowExecutionContextAccessor, WorkflowExecutionContextAccessor>();

            // ===============================
            // BUSINESS SERVICES
            // ===============================
            services.AddScoped<IMenuService, MenuService>();
            services.AddScoped<IModuleService, ModuleService>();
            services.AddScoped<IWFEvent, WFEventService>();

            // ===============================
            // 🔐 PERMISSION HANDLER
            // ===============================
            services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
                       

            return services;
        }
    }
}
