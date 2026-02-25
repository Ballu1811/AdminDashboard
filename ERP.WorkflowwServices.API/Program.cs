using AutoMapper;
using ERP.WorkflowwServices.API.Configurations;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.WorkflowwServices.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddHttpContextAccessor();

            // AutoMapper manual registration (AutoMapper 16 compatible)
            builder.Services.AddSingleton<IMapper>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<MapperProfile>();
                }, loggerFactory);

                return config.CreateMapper();
            });

            // Custom DI registrations
            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddApplicationServices(builder.Environment);

            builder.Services.AddCors(option =>
            {
                option.AddPolicy("AllowAngular",
                    //cors =>cors.WithOrigins("http://www.localhost:4200/", "http://www.localhost:4201/")
                    cors => cors.AllowAnyOrigin()
                     .AllowAnyHeader()
                     .AllowAnyMethod());
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAngular");

            app.UseHttpsRedirection();

            app.UseMiddleware<WorkflowContextMiddleware>();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
