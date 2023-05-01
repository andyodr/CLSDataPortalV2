using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Deliver.WebApi.Controllers
{
	public class BaseController : ControllerBase
	{
		private ConfigSettings? _config;
		private ApplicationDbContext? _dbc;

		protected ConfigSettings Config => _config ??= HttpContext.RequestServices.GetRequiredService<IOptions<ConfigSettings>>().Value;
		protected ApplicationDbContext Dbc => _dbc ??= HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
	}
}
