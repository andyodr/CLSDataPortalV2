using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.Users;

[Route("api/users/[controller]")]
[Authorize]
[ApiController]
public class AddController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private readonly List<int> addedHierarchies = new();
	private UserObject? _user = new();

	public AddController(ApplicationDbContext context) {
		_context = context;
	}

	[HttpGet]
	public ActionResult<JsonResult> Get() {
		var returnObject = new UserIndexGetObject { hierarchy = new(), roles = new() };
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.users, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var regions = _context.Hierarchy.Where(h => h.HierarchyLevel!.Id < 3).OrderBy(r => r.Id).ToList();
			returnObject.hierarchy.Add(new() { hierarchy = regions.First().Name, id = regions.First().Id, sub = Helper.GetSubsLevel(_context, regions.First().Id), count = 0 });
			var userRoles = _context.UserRole.OrderBy(u => u.Id);
			foreach (var role in userRoles) {
				returnObject.roles.Add(new() { id = role.Id, name = role.Name });
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
	public ActionResult<JsonResult> Post([FromBody] UserIndexDto value) {
		var returnObject = new UserIndexGetObject { data = new() };
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (_context.User.Where(u => u.UserName == value.userName).Any()) {
				throw new Exception(Resource.USERS_EXIST);
			}

			var lastUpdatedOn = DateTime.Now;

			var userC = _context.User.Add(new() {
				UserName = value.userName,
				LastName = value.lastName,
				FirstName = value.firstName,
				Department = value.department,
				Active = Helper.stringToBool(value.active),
				LastUpdatedOn = lastUpdatedOn
			});

			userC.Property("UserRoleId").CurrentValue = value.roleId;
			var user = userC.Entity;
			Helper.AddUserHierarchy(user.Id, _context, value.hierarchiesId, addedHierarchies);
			_context.SaveChanges();
			value.id = user.Id;
			returnObject.data.Add(value);
			addedHierarchies.Clear();

			Helper.AddAuditTrail(
			  _context, Resource.SECURITY,
			   "SEC-03",
			   "User Added",
			   @"ID=" + user.Id.ToString() + " / Username=" + user.UserName,
			   lastUpdatedOn,
			   _user.userId
			);

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpPut("{id}")]
	public void Put(int id, [FromBody] string value) {
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
