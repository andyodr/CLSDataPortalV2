using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Deliver.WebApi;

public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
	private readonly ApplicationDbContext _dbc;

	public CustomCookieAuthenticationEvents(ApplicationDbContext context) {
		_dbc = context;
	}

	public override async Task ValidatePrincipal(CookieValidatePrincipalContext context) {
		var userPrincipal = context.Principal;
		var lastModified = userPrincipal?.FindFirst(CustomClaimTypes.LastModified)?.Value;
		var userId = userPrincipal!.FindFirst(ClaimTypes.NameIdentifier)!.Value;
		var lastUpdatedOn = _dbc.User.Find(int.Parse(userId))?.LastUpdatedOn;
		if (string.IsNullOrEmpty(lastModified) || DateTime.Parse(lastModified) < lastUpdatedOn) {
			context.RejectPrincipal();
			await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
		}
	}

	/// <summary>
	/// Changes the default challenge behavior from a redirect to /Account/Login to a 401 Unauthorized
	/// </summary>
	public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context) {
		context.Response.StatusCode = StatusCodes.Status401Unauthorized;
		return Task.CompletedTask;
	}

	/// <summary>
	/// Changes default redirect behavior to 403 Forbidden
	/// </summary>
	public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context) {
		context.Response.StatusCode = StatusCodes.Status403Forbidden;
		return Task.CompletedTask;
	}
}

public static class CustomClaimTypes {
	public const string LastModified = "LastModified";
}
