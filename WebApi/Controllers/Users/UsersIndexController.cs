using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Users;

[ApiController]
[Route("api/users/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public class IndexController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _context;

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
		UserIndexGetObject result = new() { Data = new List<UserIndexDto>(), Hierarchy = null, Roles = new() };
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var userRoles = _context.UserRole.OrderBy(u => u.Id);
			foreach (var role in userRoles.AsNoTracking()) {
				result.Roles.Add(new() { Id = role.Id, Name = role.Name });
			}

			var users = _context.User.OrderBy(u => u.UserName);
			foreach (var user in users.Include(u => u.UserRole).AsNoTracking()) {
				var currentUser = new UserIndexDto {
					Id = user.Id,
					UserName = user.UserName,
					LastName = user.LastName,
					FirstName = user.FirstName,
					Department = user.Department,
					RoleName = user.UserRole!.Name,
					Active = user.Active
				};

				if (currentUser.UserName == _config.byPassUserName && _user.UserName != _config.byPassUserName) {

				}
				else {
					result.Data.Add(currentUser);
				}
			}

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_context, e, _user.Id));
		}
	}
}
