using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HomeController : Controller
{
	private readonly ConfigSettings _config;
	private UserObject? _user = new();

	public HomeController(IOptions<ConfigSettings> config) => _config = config.Value;

	[HttpGet("[action]/{id?}")]
	public IActionResult Index() {
		_user = CreateUserObject(User);
		if (_user is null) {
			return Unauthorized();
		}

		ViewBag.UserName = "Guess";
		ViewBag.ShowMenu = false.ToString().ToLower();
		ViewBag.ShowMenuSub = false.ToString().ToLower();
		ViewBag.TableauLink = _config.TableauLink;

		ViewBag.UserName = string.IsNullOrWhiteSpace(_user.FirstName) ? _user.UserName : _user.FirstName;

		// If Role Id is System Administrator
		if (User.IsInRole(Roles.SystemAdministrator.ToString())) {
			ViewBag.ShowMenu = true.ToString().ToLower();
		}
		// If Role Id is Region or Administrator only
		if (User.IsInRole(Roles.RegionalAdministrator.ToString()) || User.IsInRole(Roles.SystemAdministrator.ToString())) {
			ViewBag.ShowMenuSub = true.ToString().ToLower();
		}

		return Ok();
	}
}
