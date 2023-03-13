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
	private UserObject _user = null!;

	public AddController(ApplicationDbContext context) => _context = context;

	/// <summary>
	/// Get hierarchy and role data from the database.
	/// </summary>
	/// <returns>An instance of UserIndexGetObject</returns>
	[HttpGet]
	public ActionResult<UserIndexGetObject> Get() {
		var returnObject = new UserIndexGetObject { Hierarchy = new(), Roles = new() };
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var region = _context.Hierarchy
				.Where(h => h.HierarchyLevel!.Id < 3).OrderBy(r => r.Id).AsNoTrackingWithIdentityResolution().First();
			returnObject.Hierarchy.Add(new() {
				hierarchy = region.Name,
				id = region.Id,
				sub = Helper.GetSubsLevel(_context, region.Id),
				count = 0
			});
			var userRoles = _context.UserRole.OrderBy(u => u.Id);
			foreach (var role in userRoles) {
				returnObject.Roles.Add(new() { id = role.Id, name = role.Name });
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
		var returnObject = new UserIndexGetObject { Data = new() };
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

			var userEntry = _context.User.Add(new() {
				UserName = model.userName,
				LastName = model.lastName,
				FirstName = model.firstName,
				Department = model.department,
				Active = Helper.StringToBool(model.active),
				LastUpdatedOn = lastUpdatedOn
			});

			userEntry.Property("UserRoleId").CurrentValue = model.roleId;
			var user = userEntry.Entity;
			_context.SaveChanges();
			model.id = user.Id;
			returnObject.Data.Add(model);
			if (model.hierarchiesId.Count > 0) {
				// Add all the child hierarchies first before inserting UserHierarchy
				var allSelectedHierarchies = _context.Hierarchy.FromSqlRaw($@"WITH f AS
(SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed
FROM Hierarchy WHERE Id IN ({string.Join(',', model.hierarchiesId)})
UNION ALL
SELECT h.Id, h.HierarchyLevelId, h.HierarchyParentId, h.[Name], h.Active, h.LastUpdatedOn, h.IsProcessed
FROM Hierarchy h JOIN f ON h.HierarchyParentId = f.Id
WHERE h.HierarchyLevelId > 3)
SELECT DISTINCT * FROM f").AsEnumerable().Select(h => h.Id).ToArray();
				foreach (var hId in allSelectedHierarchies) {
					_context.UserHierarchy.Add(new() { UserId = user.Id, HierarchyId = hId, LastUpdatedOn = lastUpdatedOn });
				}

				_context.SaveChanges();
			}

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
