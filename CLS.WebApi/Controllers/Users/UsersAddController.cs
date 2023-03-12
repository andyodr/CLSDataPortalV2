using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

	/// <summary>
	/// Get hierarchy and role data from the database.
	/// </summary>
	/// <returns>An instance of UserIndexGetObject</returns>
	[HttpGet]
	public ActionResult<UserIndexGetObject> Get() {
		var returnObject = new UserIndexGetObject { hierarchy = new(), roles = new() };
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var region = _context.Hierarchy
				.Where(h => h.HierarchyLevel!.Id < 3).OrderBy(r => r.Id).AsNoTrackingWithIdentityResolution().First();
			returnObject.hierarchy.Add(new() {
				hierarchy = region.Name,
				id = region.Id,
				sub = Helper.GetSubsLevel(_context, region.Id),
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

	/// <summary>
	/// Create a new user in the User table and return its userId.
	/// </summary>
	/// <param name="model"></param>
	/// <returns></returns>
	[HttpPost]
	public ActionResult<UserIndexGetObject> Post(UserIndexDto model) {
		var returnObject = new UserIndexGetObject { data = new() };
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			if (_context.User.Where(u => u.UserName == model.userName).Any()) {
				return BadRequest(Resource.USERS_EXIST);
			}

			var lastUpdatedOn = DateTime.Now;

			var userC = _context.User.Add(new() {
				UserName = model.userName,
				LastName = model.lastName,
				FirstName = model.firstName,
				Department = model.department,
				Active = Helper.StringToBool(model.active),
				LastUpdatedOn = lastUpdatedOn
			});

			userC.Property("UserRoleId").CurrentValue = model.roleId;
			var user = userC.Entity;
			Helper.AddUserHierarchy(user.Id, _context, model.hierarchiesId, addedHierarchies);
			_context.SaveChanges();
			model.id = user.Id;
			returnObject.data.Add(model);
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
}
