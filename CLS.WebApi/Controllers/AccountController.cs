using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Runtime.Versioning;
using System.Security.Claims;

namespace CLS.WebApi.Controllers;

[ApiController]
[AllowAnonymous]
public class AccountController : Controller
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _dbc;

	public AccountController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_dbc = context;
	}

	[HttpPost("[action]")]
	[SupportedOSPlatform("windows")]
	public async Task<IActionResult> SignIn(
			[FromForm] string userName,
			[FromForm] string password,
			[FromForm] bool persistent = false) {
		bool continueLogin = true;
		string msgErr = Resource.USER_AUTHORIZATION_ERR;
		UserObject? user = null;

		// Checks if userName exists in database
		if (continueLogin) {
			user = Helper.CreateUserObject(_dbc, userName);
			if (user is null) {
				msgErr = Resource.VAL_USERNAME_NOT_FOUND;
				continueLogin = false;
			}
		}

		// Validates against Active Directory
		if (continueLogin) {
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
				var AD = new LdapAuthentication(_config);
				string sADReturn = AD.IsAuthenticated2(userName, password);
				if (!string.IsNullOrWhiteSpace(sADReturn)) {
					msgErr = sADReturn;
					continueLogin = false;
				}
			}
		}

		// Success
		if (continueLogin) {
			var claims = new List<Claim> {
				new(ClaimTypes.NameIdentifier, user!.userId.ToString()),
				new(ClaimTypes.Name, user.userName),
				new(ClaimTypes.Role, user.userRoleId.ToString()),
				new(CustomClaimTypes.LastModified, user.LastModified.ToString("o"))
			};

			var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "windows"));
			var properties = new AuthenticationProperties { IsPersistent = persistent };
			await HttpContext.SignInAsync(principal, properties);
			Helper.AddAuditTrail(_dbc,
				Resource.SECURITY,
				"SEC-01",
				"Login",
				@"ID=" + user.userId.ToString() + " / Username=" + user.userName,
				DateTime.Now,
				user.userId
			);

			// Start Task for login Active
			return SignIn(principal, properties);
			//return new JsonResult(new {
			//	Success = true,
			//	Id = user.userId,
			//	Name = user.userName,
			//	Role = user.userRole,
			//	TableauLink = _config.tableauLink
			//});
		}

		// Failure
		return BadRequest(msgErr);
	}

	[HttpGet("SignOut")]
	public async Task<IActionResult> GetSignOut() {
		if (User.FindFirst(ClaimTypes.NameIdentifier)?.Value is string userId) {
			int claimUserId = int.Parse(userId);
			var userRepo = _dbc.User.Where(u => u.Id == claimUserId).FirstOrDefault();
			if (userRepo is not null) {
				Helper.AddAuditTrail(_dbc,
					Resource.SECURITY,
					"SEC-02",
					"Logout",
					@"User Logout / ID=" + userId + " / Username=" + userRepo.UserName,
					DateTime.Now,
					claimUserId
				);
			}
		}

		await HttpContext.SignOutAsync();
		return SignOut();
	}
}
