using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace CLS.WebApi;

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
}

public static class CustomClaimTypes {
	public const string LastModified = "LastModified";
}
