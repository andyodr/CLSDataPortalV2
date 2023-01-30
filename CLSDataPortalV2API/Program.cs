using System.Text;
using CLSDataPortalV2API.Data;
using CLSDataPortalV2API.Extensions;
using CLSDataPortalV2API.Interfaces;
using CLSDataPortalV2API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().WithOrigins("https://localhost:4200"));

// Using the authentication middleware
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
