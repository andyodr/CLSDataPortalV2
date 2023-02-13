using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.Users;

[ApiController]
[Route("api/users/[controller]")]
[Authorize(Roles = "System Administrator")]
public class AddController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private readonly List<int> addedHierarchies = new();
	private UserObject _user = null!;

	public AddController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public ActionResult<UserIndexGetObject> Get() {
		var returnObject = new UserIndexGetObject { hierarchy = new(), roles = new() };
		try {
			if (Helper.UserAuthorization(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var regions = _context.Hierarchy.Where(h => h.HierarchyLevel!.Id < 3).OrderBy(r => r.Id).ToArray();
			returnObject.hierarchy.Add(new() {
				hierarchy = regions.First().Name,
				id = regions.First().Id,
				sub = Helper.GetSubsLevel(_context, regions.First().Id),
				count = 0 });
			var userRoles = _context.UserRole.OrderBy(u => u.Id);
			foreach (var role in userRoles) {
				returnObject.roles.Add(new() { id = role.Id, name = role.Name });
			}

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpGet("{id}")]
	public string Get(int id) => "value";

	[HttpPost]
	public ActionResult<UserIndexGetObject> Post([FromBody] UserIndexDto value) {
		var returnObject = new UserIndexGetObject { data = new() };
		try {
			if (Helper.UserAuthorization(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			if (_context.User.Where(u => u.UserName == value.userName).Any()) {
				return BadRequest(Resource.USERS_EXIST);
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

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpPut("{id}")]
	public void Put(int id, [FromBody] string value) {
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
