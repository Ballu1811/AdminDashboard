using AutoMapper;
using ERP.WorkflowwServices.API.Configurations;
using ERP.WorkflowwServices.API.Core;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ERP.WorkflowwServices.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var config = builder.Configuration;

            // ===============================
            // CONTROLLERS
            // ===============================
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });

            // ===============================
            // SWAGGER
            // ===============================
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // ===============================
            // HTTP CONTEXT
            // ===============================
            builder.Services.AddHttpContextAccessor();

            // ===============================
            // AUTOMAPPER
            // ===============================
            builder.Services.AddSingleton<IMapper>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<MapperProfile>();
                }, loggerFactory);

                return config.CreateMapper();
            });

            // ===============================
            // CUSTOM SERVICES (YOUR EXISTING)
            // ===============================
            builder.Services.AddInfrastructureServices(config);
            builder.Services.AddApplicationServices(builder.Environment);

            // ===============================
            // 🔐 JWT AUTHENTICATION
            // ===============================
            var jwtKey = config["JwtSettings:SecretKey"];

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
           .AddJwtBearer(options =>
           {
               options.RequireHttpsMetadata = false;
               options.SaveToken = true;

               options.TokenValidationParameters = new TokenValidationParameters
               {
                   ValidateIssuer = true,
                   ValidateAudience = true,
                   ValidateLifetime = true,
                   ValidateIssuerSigningKey = true,

                   ValidIssuer = config["JwtSettings:Issuer"],
                   ValidAudience = config["JwtSettings:Audience"],
                   IssuerSigningKey = new SymmetricSecurityKey(
                       Encoding.UTF8.GetBytes(jwtKey!)
                   )
               };
           });

            // ===============================
            // 🔐 AUTHORIZATION + PERMISSION
            // ===============================
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("PermissionPolicy", policy =>
                    policy.Requirements.Add(new PermissionRequirement()));
            });           

            // ===============================
            // CORS
            // ===============================
            builder.Services.AddCors(option =>
            {
                option.AddPolicy("AllowAngular",
                    //cors =>cors.WithOrigins("http://www.localhost:4200/", "http://www.localhost:4201/")
                    cors => cors.AllowAnyOrigin()
                     .AllowAnyHeader()
                     .AllowAnyMethod());
            });

            var app = builder.Build();

            // ===============================
            // 🔥 DATABASE SEEDER (IMPORTANT)
            // ===============================
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var db = services.GetRequiredService<WorkflowDbContext>();
                    db.Database.Migrate(); // 🔥 ensure DB ready

                    var seeder = services.GetRequiredService<DbSeeder>();
                    seeder.SeedAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error while seeding database");
                }
            }

                // ===============================
                // PIPELINE
                // ===============================
                if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAngular");

            app.UseHttpsRedirection();            

            // 🔥 IMPORTANT ORDER
            app.UseAuthentication();   // ✅ MUST FIRST
            app.UseAuthorization();    // ✅ THEN AUTHORIZATION

            app.UseMiddleware<WorkflowContextMiddleware>();

            app.MapControllers();

            app.Run();
        }
    }
}
