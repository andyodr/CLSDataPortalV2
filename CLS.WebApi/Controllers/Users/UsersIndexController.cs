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

	// GET: api/values
	[HttpGet]
	public ActionResult<JsonResult> Get() {
		UserIndexGetObject returnObject = new() { data = new(), hierarchy = null, roles = new() };
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
				UserIndexDto currentUser = new() {
					id = user.Id,
					userName = user.UserName,
					lastName = user.LastName,
					firstName = user.FirstName,
					department = user.Department,
					roleName = _context.UserRole.Where(u => u.Id == user.UserRole.Id).AsNoTracking().First().Name,
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

	// GET api/values/5
	[HttpGet("{id}")]
	public string Get(int id) {
		return "value";
	}

	// POST api/values
	[HttpPost]
	public void Post([FromBody] string value) {
	}

	// PUT api/values/5
	[HttpPut("{id}")]
	public void Put(int id, [FromBody] string value) {
	}

	// DELETE api/values/5
	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
