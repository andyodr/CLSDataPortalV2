using System;
using CLSDataPortalV2API.Data;
using CLSDataPortalV2API.Interfaces;
using CLSDataPortalV2API.Services;
using Microsoft.EntityFrameworkCore;

namespace CLSDataPortalV2API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<DataContext>(options =>
            {
                //Use SQL Server for production 
                options.UseSqlite(config.GetConnectionString("DefaultConnection"));
            });

            // Add CORS Policy
            services.AddCors();
    
            // Add JWT Authentication
            services.AddScoped<ITokenService, TokenService>();

            return services;
        }
    }
}