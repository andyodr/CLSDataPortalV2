using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CLS.WebApi.Controllers;

public class HomeController : Controller
{
	private readonly ConfigurationObject _config;
	private UserObject _user = new();

	public HomeController(IOptions<ConfigurationObject> config) {
		_config = config.Value;
	}

	[Authorize]
	[HttpGet("", Name = "default")]
	[HttpGet("[controller]")]
	[HttpGet("[controller]/[action]")]
	[HttpGet("[controller]/[action]/{id?}")]
	public IActionResult Index() {
		_user = Helper.UserAuthorization(User);
		if (_user == null)
			return RedirectToAction(nameof(AccountController.Login), "Account");

		ViewBag.UserName = "Guess";
		ViewBag.ShowMenu = false.ToString().ToLower();
		ViewBag.ShowMenuSub = false.ToString().ToLower();
		ViewBag.TableauLink = _config.tableauLink;

		if (String.IsNullOrWhiteSpace(_user.firstName))
			ViewBag.UserName = _user.userName;
		else
			ViewBag.UserName = _user.firstName;


		// If Role Id is System Administrator
		if (_user.userRoleId == (int)Helper.userRoles.systemAdministrator) {
			ViewBag.ShowMenu = true.ToString().ToLower();
		}
		// If Role Id is Region or Administrator only
		if (_user.userRoleId != (int)Helper.userRoles.powerUser) {
			ViewBag.ShowMenuSub = true.ToString().ToLower();
		}

		return View();
	}

	[Route("{*catchall}", Name = "Error")]
	public IActionResult Error() {
		return RedirectToAction(nameof(HomeController.Index), "Home");
	}
}
