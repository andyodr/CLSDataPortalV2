using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Users;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public sealed class UsersController : BaseController
{
	/// <returns>Complete list of authorized users</returns>
	[HttpGet]
	public ActionResult<UserIndexGetObject> Get() {
		UserIndexGetObject result = new() { Data = [], Hierarchy = null, Roles = [] };
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
			return BadRequest(Dbc.ErrorProcessing(e, user.Id));
		}
	}
}
