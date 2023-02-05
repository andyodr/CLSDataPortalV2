using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CLS.WebApi.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _context;

	public AccountController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_context = context;
	}

	[HttpGet]
	public IActionResult Login(string returnUrl) {
		HttpContext.SignOutAsync("Cookies");
		return View();
	}

	[HttpPost]
	public async Task<IActionResult> Login(string userName, string password, string returnUrl) {
		bool bContinue = true;
		string msgErr = Resource.USER_AUTHORIZATION_ERR;

		UserObject user = new();

		if (String.IsNullOrWhiteSpace(userName) || String.IsNullOrWhiteSpace(password)) {
			msgErr = Resource.VAL_USERNAME_PASSWORD;
			bContinue = false;
		}

		// Checks if userName exists in database
		if (bContinue) {
			user = Helper.setUser(userName);
			if (user == null) {
				msgErr = Resource.VAL_USERNAME_NOT_FOUND;
				bContinue = false;
			}
			else {
				if (Helper.userCookies.ContainsKey(user.userId.ToString()))
					Helper.userCookies.Remove(user.userId.ToString());

				Helper.userCookies.Add(user.userId.ToString(), user);
			}
		}

		// Validates against Active Directory
		if (bContinue) {

			bool bIsByPass = false;
			// **************** DELETE ***********************************
			//bIsByPass = true;
			// **************** DELETE ***********************************

			//Validates ByPass user
			if ((userName == _config.byPassUserName) &&
				(password == _config.byPassUserPassword)) {
				bIsByPass = true;
			}

			// Check Active Directory if User is NOT ByPassUser 
			if (!bIsByPass) {
				LdapAuthentication AD = new LdapAuthentication();
				string sADReturn = AD.IsAuthenticated2(userName, password);

				if (!String.IsNullOrWhiteSpace(sADReturn)) {
					msgErr = sADReturn;
					bContinue = false;
				}
			}
		}

		// Success
		if (bContinue) {
			var claims = new List<Claim> {
				new Claim("userId", user.userId.ToString()),
				new Claim("name", user.userName)
			};
			var id = new ClaimsIdentity(claims, "local", "name", "role");

			await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(id));

			Helper.addAuditTrail(
			  Resource.SECURITY,
			   "SEC-01",
			   "Login",
			   @"ID=" + user.userId.ToString() + " / Username=" + user.userName,
			   DateTime.Now,
			   user.userId
			);

			// Start Task for login Active

			return RedirectToAction(nameof(HomeController.Index), "Home");
		}

		// Failure
		ViewBag.Error = msgErr;
		return View();
	}

	public async Task<IActionResult> Logoff() {
		var user = User.Claims.Where(c => c.Type == "userId").ToList();
		if (user.Count() > 0) {
			string userId = user.First().Value;
			if (Helper.userCookies.ContainsKey(userId))
				Helper.userCookies.Remove(userId);

			int nUserId = Int32.Parse(userId);
			var userRepo = _context.User.Where(u => u.Id == nUserId).FirstOrDefault();
			if (userRepo != null) {
				Helper.addAuditTrail(
				  Resource.SECURITY,
				   "SEC-02",
				   "Logout",
				   @"User Logout / ID=" + userId + " / Username=" + userRepo.UserName,
				   DateTime.Now,
				   nUserId
				);
			}

			await HttpContext.SignOutAsync("Cookies");

		}
		return RedirectToAction(nameof(AccountController.Login), "Account");
	}
}
