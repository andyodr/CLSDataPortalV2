using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Users;

[ApiController]
[Route("api/users/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public sealed class IndexController : BaseController
{
	/// <summary>
	/// Get complete listing of authorized users.
	/// </summary>
	/// <returns></returns>
	[HttpGet]
	public ActionResult<UserIndexGetObject> Get() {
		UserIndexGetObject result = new() { Data = new List<UserIndexDto>(), Hierarchy = null, Roles = new() };
        var user = CreateUserObject(User);
        if (user == null) {
			return Unauthorized();
		}

		try {
			var userRoles = Dbc.UserRole.OrderBy(u => u.Id);
			foreach (var role in userRoles.AsNoTracking()) {
				result.Roles.Add(new() { Id = role.Id, Name = role.Name });
			}

			var users = Dbc.User.OrderBy(u => u.UserName);
			foreach (var u in users.Include(u => u.UserRole).AsNoTracking()) {
                UserIndexDto currentUser = new() {
					Id = u.Id,
					UserName = u.UserName,
					LastName = u.LastName,
					FirstName = u.FirstName,
					Department = u.Department,
					RoleName = u.UserRole!.Name,
					Active = u.Active
				};

				if (currentUser.UserName == Config.BypassUserName && user.UserName != Config.BypassUserName) {

				}
				else {
					result.Data.Add(currentUser);
				}
			}

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, user.Id));
		}
	}
}
