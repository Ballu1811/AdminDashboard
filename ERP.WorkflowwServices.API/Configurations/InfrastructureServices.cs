using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERP.WorkflowwServices.API.Configurations
{
    public static class InfrastructureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<WorkflowDbContext>(options =>
            {
                options.UseSqlServer(config.GetConnectionString("DefaultConnection"));

                options.EnableSensitiveDataLogging();

                options.LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information);
            });

            return services;
        }
    }
}
