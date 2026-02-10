using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Configurations
{
    public static class InfrastructureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<WorkflowDbContext>(options => options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

            return services;
        }
    }
}
