using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Versioning;
using System.Security.Claims;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class AccountController : Controller
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _dbc;

	public AccountController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_dbc = context;
	}

	public class RequestDto
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
	[AllowAnonymous]
	[SupportedOSPlatform("windows")]
	public async Task<IActionResult> SignIn([FromForm] RequestDto form) {
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
			if (user!.UserName.Equals(_config.byPassUserName, StringComparison.CurrentCultureIgnoreCase)
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
				new(ClaimTypes.NameIdentifier, user!.Id.ToString(), ClaimValueTypes.Integer),
				new(ClaimTypes.Name, user.UserName),
				new(ClaimTypes.Role, user.RoleId.ToString()),
				new(CustomClaimTypes.LastModified, user.LastModified.ToString("o"), ClaimValueTypes.DateTime)
			};

			var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
			var properties = new AuthenticationProperties();
			if (form.Persistent ?? false) {
				properties.IsPersistent = true;
				properties.ExpiresUtc = DateTime.Today.AddMonths(6);
			}

			await HttpContext.SignInAsync(principal, properties);

			// cookie with same expiration but readable by scripts for purposes of determining signed-in status
			HttpContext.Response.Cookies.Append("AuthPresent", properties.ExpiresUtc?.ToString("u") ?? "",
				new CookieOptions { Expires = properties.ExpiresUtc, IsEssential = true });
			AddAuditTrail(_dbc,
				Resource.SECURITY,
				"SEC-01",
				"Login",
				@"ID=" + user.Id.ToString() + " / Username=" + user.UserName,
				DateTime.Now,
				user.Id
			);

			return Ok(new {
				user.Id,
				user.UserName,
				user.FirstName,
				user.LastName,
				user.Department,
				user.Role,
				user.RoleId,
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
				AddAuditTrail(_dbc,
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
		HttpContext.Response.Cookies.Delete("AuthPresent");
		return SignOut();
	}

	private static UserObject? CreateDetailedUserObject(ApplicationDbContext dbc, string userName) {
		var entity = dbc.User
			.Where(u => u.UserName == userName)
			.Include(u => u.UserRole)
			.Include(u => u.UserCalendarLocks)
			.Include(u => u.UserHierarchies)
			.AsSplitQuery()
			.AsNoTrackingWithIdentityResolution().Single();
		var localUser = new UserObject {
			Id = entity.Id,
			RoleId = (Roles)entity.UserRole!.Id,
			UserName = entity.UserName,
			FirstName = entity.FirstName,
			LastName = entity.LastName,
			Department = entity.Department,
			Role = entity.UserRole.Name,
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
