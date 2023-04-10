using CLS.WebApi;
using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddMvc();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services
	.AddEndpointsApiExplorer()
	.AddSwaggerGen(options => {
		var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
		options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
	})
	.Configure<ConfigurationObject>(builder.Configuration.GetSection(ConfigurationObject.Section))
	.AddSqlServer<ApplicationDbContext>(builder.Configuration.GetConnectionString("DefaultConnection"))
	.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
if (Directory.Exists(@"C:\inetpub")) {
	builder.Services
		.AddDataProtection()
		.PersistKeysToFileSystem(new DirectoryInfo(@"C:\inetpub\keys"));
}

builder.Services
	.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options => {
		options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
		options.SlidingExpiration = true;
		options.EventsType = typeof(CustomCookieAuthenticationEvents);
	});

builder.Services.AddScoped<CustomCookieAuthenticationEvents>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
	app.UseSwagger();
	app.UseSwaggerUI();
}
else {
	app.UseExceptionHandler("/Error");
	app.MapGet("Error", () => Results.Problem());
}

app.UseAuthentication();
app.UseCookiePolicy(new() { MinimumSameSitePolicy = SameSiteMode.Lax });
app.UseAuthorization();
app.MapControllers();
app.Run();
