using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Versioning;
using System.Security.Claims;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers;

[ApiController]
[Route("api")]
[AllowAnonymous]
public sealed class AccountController : BaseController
{
	public sealed class RequestDto
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
	public async Task<IActionResult> SignIn([FromForm] RequestDto form, CancellationToken token) {
		bool continueLogin = true;
		string authenticationType = string.Empty;
		string msgErr = Resource.USER_AUTHORIZATION_ERR;
		UserObject? user = null;

		// Checks if userName exists in database
		if (continueLogin) {
			user = await CreateDetailedUserObject(form.UserName, token);
			if (user is null) {
				msgErr = Resource.VAL_USERNAME_NOT_FOUND;
				continueLogin = false;
			}
		}

		// Validates against Active Directory
		if (continueLogin) {
			if (user!.UserName.Equals(Config.BypassUserName, StringComparison.CurrentCultureIgnoreCase)
					&& form.Password == Config.BypassUserPassword) {
				authenticationType = "bypass";
			}
			else {
				authenticationType = "windows";
				var AD = new LdapAuthentication(Config);
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
			Response.Cookies.Append("AuthPresent", properties.ExpiresUtc?.ToString("u") ?? "",
				new CookieOptions { Expires = properties.ExpiresUtc, IsEssential = true });
			Dbc.AddAuditTrail(Resource.SECURITY, "SEC-01",
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
				Config.TableauLink,
				Persist = form.Persistent
			});
		}

		return ValidationProblem(msgErr);
	}

	/// <summary>
	/// Sign out of the application, removing the authentication cookie.
	/// </summary>
	[HttpGet("SignOut")]
	public async Task<IActionResult> GetSignOut() {
		if (User.FindFirst(ClaimTypes.NameIdentifier)?.Value is string userId) {
			int claimUserId = int.Parse(userId);
			var userRepo = Dbc.User.Where(u => u.Id == claimUserId).FirstOrDefault();
			if (userRepo is not null) {
				Dbc.AddAuditTrail(
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
		Response.Cookies.Delete("AuthPresent");
		return SignOut();
	}

	[HttpGet("[action]")]
	public async Task<IActionResult> CanConnect(CancellationToken token) {
		try {
			return Ok(await Dbc.Database.CanConnectAsync(token));
		}
		catch (TaskCanceledException) {
			return StatusCode(499);
		}
	}

	private async Task<UserObject?> CreateDetailedUserObject(string userName, CancellationToken token) {
		var entity = await Dbc.User
			.Where(u => u.UserName == userName)
			.Include(u => u.UserRole)
			.Include(u => u.UserCalendarLocks)
			.Include(u => u.UserHierarchies)
			.AsSplitQuery()
			.AsNoTrackingWithIdentityResolution().FirstAsync(token);
		UserObject user = new() {
			Id = entity.Id,
			RoleId = (Roles)entity.UserRole!.Id,
			UserName = entity.UserName,
			FirstName = entity.FirstName,
			LastName = entity.LastName,
			Department = entity.Department,
			Role = entity.UserRole.Name,
			LastModified = entity.LastUpdatedOn
		};
		user.calendarLockIds.AddRange(entity.UserCalendarLocks!.Select(c => new UserCalendarLocks {
			CalendarId = c.CalendarId,
			LockOverride = c.LockOverride
		}));
		user.hierarchyIds.AddRange(entity.UserHierarchies!.Select(h => h.Id));
		return user;
	}
}
