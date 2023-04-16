using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Users;

[ApiController]
[Route("api/users/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public class EditController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;

	public EditController(ApplicationDbContext context) => _dbc = context;

	/// <summary>
	/// Get hierarchy and role data, and user data for a subset of users.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	[HttpGet("{id:min(1)}")]
	public ActionResult<UserIndexGetObject> Get(int id) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			UserIndexGetObject result = new() { Data = new List<UserIndexDto>(), Hierarchy = new(), Roles = new() };
			var regions = _dbc.Hierarchy.Where(h => h.HierarchyLevel!.Id == 1).ToArray();
			result.Hierarchy.Add(new() {
				Hierarchy = regions.First().Name,
				Id = regions.First().Id,
				Sub = GetSubsLevel(_dbc, regions.First().Id),
				Count = 0
			});

			var userRoles = _dbc.UserRole.OrderBy(u => u.Id);
			foreach (var role in userRoles) {
				result.Roles.Add(new() { Id = role.Id, Name = role.Name });
			}

			var users = _dbc.User
				.Where(u => u.Id == id)
				.OrderBy(u => u.UserName)
				.Include(u => u.UserRole)
				.Include(u => u.UserHierarchies)!
				.ThenInclude(uh => uh.Hierarchy);
			foreach (var user in users) {
				UserIndexDto currentUser = new() {
					HierarchiesId = new(),
					Id = user.Id,
					UserName = user.UserName,
					LastName = user.LastName,
					FirstName = user.FirstName,
					Department = user.Department,
					RoleName = user.UserRole?.Name ?? string.Empty,
					RoleId = user.UserRole?.Id ?? -1,
					Active = user.Active
				};

				foreach (var userH in user.UserHierarchies!) {
					currentUser.HierarchiesId.Add(userH.Hierarchy!.Id);
					currentUser.HierarchyName = userH.Hierarchy!.Name;
				}

				if (user.Id == (int)Roles.PowerUser) {
					if (_user.Id == (int)Roles.PowerUser) {
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
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}

	/// <summary>
	/// Modify user details for a specified user ID.
	/// </summary>
	[HttpPut("{id:min(1)}")]
	public ActionResult<UserIndexGetObject> Put(int id, UserIndexDto body) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var user = _dbc.User.Find(id);
			if (user == null) {
				return ValidationProblem("User ID not found.");
			}

			if (body.UserName != user.UserName) {
				if (_dbc.User.Where(u => u.UserName == body.UserName).Any()) {
					return ValidationProblem(Resource.USERS_EXIST);
				}
			}

			var lastUpdatedOn = DateTime.Now;
			user.UserName = body.UserName;
			user.LastName = body.LastName;
			user.FirstName = body.FirstName;
			user.Department = body.Department;
			user.Active = body.Active;
			user.LastUpdatedOn = lastUpdatedOn;
			_dbc.Entry(user).Property("UserRoleId").CurrentValue = body.RoleId;

			if (body.HierarchiesId.Count > 0) {
				// UI only shows hierarchyLevel < 5, but we need to add all the child hierarchies as well
				var allSelectedHierarchies = _dbc.Hierarchy.FromSqlRaw($@"WITH f AS
(SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed
FROM Hierarchy WHERE HierarchyLevelId < 4 AND Id IN ({string.Join(',', body.HierarchiesId)})
UNION ALL
SELECT h.Id, h.HierarchyLevelId, h.HierarchyParentId, h.[Name], h.Active, h.LastUpdatedOn, h.IsProcessed
FROM Hierarchy h JOIN f ON h.HierarchyParentId = f.Id
WHERE h.HierarchyLevelId > 3)
SELECT DISTINCT * FROM f").AsEnumerable().Select(h => h.Id).ToArray();
				_dbc.UserHierarchy
					.Where(h => h.UserId == id && !allSelectedHierarchies.Contains(h.HierarchyId))
					.ExecuteDelete();
				var remaining = _dbc.UserHierarchy.Where(h => h.UserId == id).Select(h => h.HierarchyId).ToArray();
				foreach (var hId in allSelectedHierarchies) {
					if (!remaining.Contains(hId)) {
						_dbc.UserHierarchy.Add(new() { UserId = id, HierarchyId = hId, LastUpdatedOn = lastUpdatedOn });
					}
				}

				_dbc.SaveChanges();
			}
			else {
				_dbc.UserHierarchy.Where(h => h.UserId == id).ExecuteDelete();
			}

			body.Id = id;
			UserIndexGetObject result = new() { Data = new UserIndexDto[] { body } };
			AddAuditTrail(_dbc,
				Resource.SECURITY,
				"SEC-04",
				"User Updated",
				@"ID=" + id.ToString() + " / Username=" + user.UserName,
				lastUpdatedOn,
				_user.Id
			);

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
