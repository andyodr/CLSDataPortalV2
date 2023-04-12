using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HomeController : Controller
{
	private readonly ConfigurationObject _config;
	private UserObject? _user = new();

	public HomeController(IOptions<ConfigurationObject> config) => _config = config.Value;

	[HttpGet("[action]/{id?}")]
	public IActionResult Index() {
		_user = CreateUserObject(User);
		if (_user is null) {
			return Unauthorized();
		}

		ViewBag.UserName = "Guess";
		ViewBag.ShowMenu = false.ToString().ToLower();
		ViewBag.ShowMenuSub = false.ToString().ToLower();
		ViewBag.TableauLink = _config.tableauLink;

		if (string.IsNullOrWhiteSpace(_user.FirstName)) {
			ViewBag.UserName = _user.UserName;
		}
		else {
			ViewBag.UserName = _user.FirstName;
		}

		// If Role Id is System Administrator
		if (User.IsInRole(Roles.SystemAdministrator.ToString())) {
			ViewBag.ShowMenu = true.ToString().ToLower();
		}
		// If Role Id is Region or Administrator only
		if (User.IsInRole(Roles.RegionalAdministrator.ToString()) || User.IsInRole(Roles.SystemAdministrator.ToString())) {
			ViewBag.ShowMenuSub = true.ToString().ToLower();
		}

		return View();
	}
}
