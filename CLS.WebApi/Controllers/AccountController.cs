using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Versioning;
using System.Security.Claims;

namespace CLS.WebApi.Controllers;

[ApiController]
[Route("api")]
[AllowAnonymous]
public class AccountController : Controller
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _dbc;

	public AccountController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_dbc = context;
	}

	public class RequestModel
	{
		[Required]
		public string UserName { get; set; } = null!;

		[Required]
		public string Password { get; set; } = null!;

		public bool? Persistent { get; set; }
	}

	/// <summary>
	/// Authenticate the user and sign in to the application.
	/// </summary>
	[HttpPost("[action]")]
	[SupportedOSPlatform("windows")]
	public async Task<IActionResult> SignIn([FromForm] RequestModel form) {
		bool continueLogin = true;
		string authenticationType = string.Empty;
		string msgErr = Resource.USER_AUTHORIZATION_ERR;
		UserObject? user = null;

		// Checks if userName exists in database
		if (continueLogin) {
			user = CreateDetailedUserObject(_dbc, form.UserName);
			if (user is null) {
				msgErr = Resource.VAL_USERNAME_NOT_FOUND;
				continueLogin = false;
			}
		}

		// Validates against Active Directory
		if (continueLogin) {
			if (user!.userName.Equals(_config.byPassUserName, StringComparison.CurrentCultureIgnoreCase)
					&& form.Password == _config.byPassUserPassword) {
				authenticationType = "bypass";
			}
			else {
				authenticationType = "windows";
				var AD = new LdapAuthentication(_config);
				string sADReturn = AD.IsAuthenticated2(form.UserName, form.Password);
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
				new(ClaimTypes.Role, user.userRole),
				new(CustomClaimTypes.LastModified, user.LastModified.ToString("o"))
			};

			var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
			var properties = new AuthenticationProperties { IsPersistent = form.Persistent ?? false };
			await HttpContext.SignInAsync(principal, properties);
			Helper.AddAuditTrail(_dbc,
				Resource.SECURITY,
				"SEC-01",
				"Login",
				@"ID=" + user.userId.ToString() + " / Username=" + user.userName,
				DateTime.Now,
				user.userId
			);

			return Ok(new {
				Id = user.userId,
				Name = user.userName,
				Role = user.userRole,
				TableauLink = _config.tableauLink,
				Persist = form.Persistent
			});
		}

		return ValidationProblem(msgErr);
	}

	/// <summary>
	/// Sign out of the application, removing the authentication cookie.
	/// </summary>
	/// <returns></returns>
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

	[NonAction]
	public static UserObject? CreateDetailedUserObject(ApplicationDbContext dbc, string userName) {
		var entity = dbc.User
			.Where(u => u.UserName == userName)
			.Include(u => u.UserRole)
			.Include(u => u.UserCalendarLocks)
			.Include(u => u.UserHierarchies)
			.AsSplitQuery()
			.AsNoTrackingWithIdentityResolution().Single();
		var localUser = new UserObject {
			userId = entity.Id,
			userRoleId = entity.UserRole!.Id,
			userName = entity.UserName,
			firstName = entity.FirstName,
			userRole = entity.UserRole.Name,
			LastModified = entity.LastUpdatedOn
		};
		localUser.calendarLockIds.AddRange(entity.UserCalendarLocks!.Select(c => new UserCalendarLocks {
			CalendarId = c.CalendarId,
			LockOverride = c.LockOverride
		}));
		localUser.hierarchyIds.AddRange(entity.UserHierarchies!.Select(h => h.Id));
		return localUser;
	}
}
