using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CLS.WebApi.Controllers.Users;

[Route("api/users/[controller]")]
[Authorize]
[ApiController]
public class IndexController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _context;
	private UserObject _user = new();

	public IndexController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_context = context;
	}

	[HttpGet]
	public ActionResult<JsonResult> Get() {
		var returnObject = new UserIndexGetObject { data = new(), hierarchy = null, roles = new() };
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.users, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var userRoles = _context.UserRole.OrderBy(u => u.Id);
			foreach (var role in userRoles.AsNoTracking()) {
				returnObject.roles.Add(new() { id = role.Id, name = role.Name });
			}

			var users = _context.User.OrderBy(u => u.UserName);
			foreach (var user in users.Include(u => u.UserRole).AsNoTracking()) {
				var currentUser = new UserIndexDto {
					id = user.Id,
					userName = user.UserName,
					lastName = user.LastName,
					firstName = user.FirstName,
					department = user.Department,
					roleName = user.UserRole.Name,
					active = Helper.boolToString(user.Active)
				};

				if (currentUser.userName == _config.byPassUserName && _user.userName != _config.byPassUserName) {

				}
				else {
					returnObject.data.Add(currentUser);
				}
			}

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpGet("{id}")]
	public string Get(int id) {
		return "value";
	}

	[HttpPost]
	public void Post([FromBody] string value) {
	}

	[HttpPut("{id}")]
	public void Put(int id, [FromBody] string value) {
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
