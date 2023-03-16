using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CLS.WebApi.Controllers.Users;

[ApiController]
[Route("api/users/[controller]")]
[Authorize(Roles = "System Administrator")]
public class EditController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public EditController(ApplicationDbContext context) => _context = context;

	/// <summary>
	/// Get hierarchy and role data, and user data for a subset of users.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	[HttpGet("{id}")]
	public ActionResult<UserIndexGetObject> Get(int id) {
		var result = new UserIndexGetObject { Data = new(), Hierarchy = new(), Roles = new() };
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var regions = _context.Hierarchy.Where(h => h.HierarchyLevel!.Id == 1).ToArray();
			result.Hierarchy.Add(new() {
				Hierarchy = regions.First().Name,
				Id = regions.First().Id,
				Sub = Helper.GetSubsLevel(_context, regions.First().Id),
				Count = 0
			});

			var userRoles = _context.UserRole.OrderBy(u => u.Id);
			foreach (var role in userRoles) {
				result.Roles.Add(new() { id = role.Id, name = role.Name });
			}

			var users = _context.User
				.Where(u => u.Id == id)
				.OrderBy(u => u.UserName)
				.Include(u => u.UserRole)
				.Include(u => u.UserHierarchies)!
				.ThenInclude(uh => uh.Hierarchy);
			foreach (var user in users) {
				var currentUser = new UserIndexDto {
					hierarchiesId = new(),
					id = user.Id,
					userName = user.UserName,
					lastName = user.LastName,
					firstName = user.FirstName,
					department = user.Department,
					roleName = user.UserRole?.Name ?? string.Empty,
					roleId = user.UserRole?.Id ?? -1,
					active = Helper.boolToString(user.Active)
				};

				foreach (var userH in user.UserHierarchies!) {
					currentUser.hierarchiesId.Add(userH.Hierarchy!.Id);
					currentUser.hierarchyName = userH.Hierarchy!.Name;
				}

				if (user.Id == (int)Helper.userRoles.powerUser) {
					if (_user.userId == (int)Helper.userRoles.powerUser) {
						result.Data.Add(currentUser);
					}
				}
				else {
					result.Data.Add(currentUser);
				}
			}

			return result;
		}
		catch (Exception e) {
			int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	/// <summary>
	/// Modify user details for a specified user ID.
	/// </summary>
	[HttpPut("{id}")]
	public ActionResult<UserIndexGetObject> Put(int id, UserIndexDto model) {
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var returnObject = new UserIndexGetObject { Data = new() };
			var user = _context.User.Find(id);
			if (user == null) {
				return ValidationProblem("User ID not found.");
			}

			if (model.userName != user.UserName) {
				if (_context.User.Where(u => u.UserName == model.userName).Any()) {
					return ValidationProblem(Resource.USERS_EXIST);
				}
			}

			var lastUpdatedOn = DateTime.Now;
			user.UserName = model.userName;
			user.LastName = model.lastName;
			user.FirstName = model.firstName;
			user.Department = model.department;
			user.Active = Helper.StringToBool(model.active);
			user.LastUpdatedOn = lastUpdatedOn;
			_context.Entry(user).Property("UserRoleId").CurrentValue = model.roleId;

			if (model.hierarchiesId.Count > 0) {
				// UI only shows hierarchyLevel < 4, but we need to add all the child hierarchies as well
				var allSelectedHierarchies = _context.Hierarchy.FromSqlRaw($@"WITH f AS
(SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed
FROM Hierarchy WHERE HierarchyLevelId < 4 AND Id IN ({string.Join(',', model.hierarchiesId)})
UNION ALL
SELECT h.Id, h.HierarchyLevelId, h.HierarchyParentId, h.[Name], h.Active, h.LastUpdatedOn, h.IsProcessed
FROM Hierarchy h JOIN f ON h.HierarchyParentId = f.Id
WHERE h.HierarchyLevelId > 3)
SELECT DISTINCT * FROM f").AsEnumerable().Select(h => h.Id).ToArray();
				_context.UserHierarchy
					.Where(h => h.UserId == id && !allSelectedHierarchies.Contains(h.HierarchyId))
					.ExecuteDelete();
				var remaining = _context.UserHierarchy.Where(h => h.UserId == id).Select(h => h.HierarchyId).ToArray();
				foreach (var hId in allSelectedHierarchies) {
					if (!remaining.Contains(hId)) {
						_context.UserHierarchy.Add(new() { UserId = id, HierarchyId = hId, LastUpdatedOn = lastUpdatedOn });
					}
				}

				_context.SaveChanges();
			}
			else {
				_context.UserHierarchy.Where(h => h.UserId == id).ExecuteDelete();
			}

			returnObject.Data.Add(model);

			Helper.AddAuditTrail(_context,
				Resource.SECURITY,
				"SEC-04",
				"User Updated",
				@"ID=" + id.ToString() + " / Username=" + user.UserName,
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
