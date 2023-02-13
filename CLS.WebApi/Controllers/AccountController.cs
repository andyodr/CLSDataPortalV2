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
	private readonly ApplicationDbContext _context;

	public AccountController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_context = context;
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

		if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password)) {
			msgErr = Resource.VAL_USERNAME_PASSWORD;
			continueLogin = false;
		}

		// Checks if userName exists in database
		if (continueLogin) {
			user = Helper.CreateUserObject(_context, userName);
			if (user is null) {
				msgErr = Resource.VAL_USERNAME_NOT_FOUND;
				continueLogin = false;
			}
			else {
				Helper.userCookies[user.userId.ToString()] = user;
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

			await HttpContext.SignInAsync(
				new ClaimsPrincipal(new ClaimsIdentity(claims, "windows")),
				new AuthenticationProperties { IsPersistent = persistent });

			Helper.AddAuditTrail(_context,
				Resource.SECURITY,
				"SEC-01",
				"Login",
				@"ID=" + user.userId.ToString() + " / Username=" + user.userName,
				DateTime.Now,
				user.userId
			);

			// Start Task for login Active
			return new JsonResult(new {
				Success = true,
				Id = user.userId,
				Name = user.userName,
				Role = user.userRole,
				TableauLink = _config.tableauLink
			});
		}

		// Failure
		return new JsonResult(new {
			Success = false,
			Message = msgErr
		});
	}

	[HttpGet("[action]")]
	public async Task<IActionResult> Logoff() {
		string? claimUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (claimUserId is string userId) {
			var user = User.Claims.Where(c => c.Type == "userId").ToArray();
			Helper.userCookies.Remove(userId);

			int nUserId = int.Parse(userId);
			var userRepo = _context.User.Where(u => u.Id == nUserId).FirstOrDefault();
			if (userRepo is not null) {
				Helper.AddAuditTrail(_context,
					Resource.SECURITY,
					"SEC-02",
					"Logout",
					@"User Logout / ID=" + userId + " / Username=" + userRepo.UserName,
					DateTime.Now,
					nUserId
				);
			}
		}

		await HttpContext.SignOutAsync();
		return RedirectToAction(nameof(AccountController.SignIn), "Account");
	}
}
