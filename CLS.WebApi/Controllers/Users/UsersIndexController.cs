using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CLS.WebApi.Controllers.Users;

[ApiController]
[Route("api/users/[controller]")]
[Authorize(Roles = "System Administrator")]
public class IndexController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public IndexController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_context = context;
	}

	/// <summary>
	/// Get complete listing of authorized users.
	/// </summary>
	/// <returns></returns>
	[HttpGet]
	public ActionResult<UserIndexGetObject> Get() {
		var returnObject = new UserIndexGetObject { Data = new(), Hierarchy = null, Roles = new() };
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var userRoles = _context.UserRole.OrderBy(u => u.Id);
			foreach (var role in userRoles.AsNoTracking()) {
				returnObject.Roles.Add(new() { id = role.Id, name = role.Name });
			}

			var users = _context.User.OrderBy(u => u.UserName);
			foreach (var user in users.Include(u => u.UserRole).AsNoTracking()) {
				var currentUser = new UserIndexDto {
					id = user.Id,
					userName = user.UserName,
					lastName = user.LastName,
					firstName = user.FirstName,
					department = user.Department,
					roleName = user.UserRole!.Name,
					active = Helper.boolToString(user.Active)
				};

				if (currentUser.userName == _config.byPassUserName && _user.userName != _config.byPassUserName) {

				}
				else {
					returnObject.Data.Add(currentUser);
				}
			}

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}
}
