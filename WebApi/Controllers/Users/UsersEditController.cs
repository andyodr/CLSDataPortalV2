using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Users;

[ApiController]
[Route("api/users/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public sealed class EditController : BaseController
{
	/// <returns>User information including roles and hierarchy assignments</returns>
	[HttpGet("{id:min(1)}")]
	public ActionResult<UserIndexGetObject> Get(int id) {
		if (CreateUserObject(User) is not UserDto _user) {
			return Unauthorized();
		}

		try {
			UserIndexGetObject result = new() { Data = [], Hierarchy = [], Roles = [] };
			var root = Dbc.Hierarchy.Where(h => h.HierarchyParentId == null).First();
			result.Hierarchy.Add(new() {
				Hierarchy = root.Name,
				Id = root.Id,
				Sub = AddController.GetSubsLevel(Dbc, root.Id),
				Count = 0
			});

			var userRoles = Dbc.UserRole.OrderBy(u => u.Id);
			foreach (var role in userRoles) {
				result.Roles.Add(new() { Id = role.Id, Name = role.Name });
			}

			var users = Dbc.User
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
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}

	/// <summary>
	/// Modify user details for a specified user ID.
	/// </summary>
	[HttpPut("{id:min(1)}")]
	public ActionResult<UserIndexGetObject> Put(int id, UserIndexDto body) {
		if (CreateUserObject(User) is not UserDto _user) {
			return Unauthorized();
		}

		try {
			var user = Dbc.User.Find(id);
			if (user == null) {
				return ValidationProblem("User ID not found.");
			}

			if (body.UserName != user.UserName) {
				if (Dbc.User.Where(u => u.UserName == body.UserName).Any()) {
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
			Dbc.Entry(user).Property("UserRoleId").CurrentValue = body.RoleId;

			if (body.HierarchiesId.Count > 0) {
				// UI only shows hierarchyLevel < 5, but we need to add all the child hierarchies as well
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection
				var allSelectedHierarchies = Dbc.Hierarchy.FromSqlRaw($"""
					WITH r AS
						(SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed
						FROM Hierarchy WHERE HierarchyLevelId < 5 AND Id IN ({string.Join(',', body.HierarchiesId)})
						UNION ALL
						SELECT h.Id, h.HierarchyLevelId, h.HierarchyParentId, h.[Name], h.Active, h.LastUpdatedOn, h.IsProcessed
						FROM Hierarchy h JOIN r ON h.HierarchyParentId = r.Id
						WHERE h.HierarchyLevelId > 4)
					SELECT DISTINCT * FROM r
					""").AsEnumerable().Select(h => h.Id).ToArray();
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection
				Dbc.UserHierarchy
					.Where(h => h.UserId == id && !allSelectedHierarchies.Contains(h.HierarchyId))
					.ExecuteDelete();
				var remaining = Dbc.UserHierarchy.Where(h => h.UserId == id).Select(h => h.HierarchyId).ToArray();
				foreach (var hId in allSelectedHierarchies) {
					if (!remaining.Contains(hId)) {
						Dbc.UserHierarchy.Add(new() { UserId = id, HierarchyId = hId, LastUpdatedOn = lastUpdatedOn });
					}
				}

				Dbc.SaveChanges();
			}
			else {
				Dbc.UserHierarchy.Where(h => h.UserId == id).ExecuteDelete();
			}

			body.Id = id;
			UserIndexGetObject result = new() { Data = [body] };
			Dbc.AddAuditTrail(Resource.SECURITY, "SEC-04",
				"User Updated",
				@"ID=" + id.ToString() + " / Username=" + user.UserName,
				lastUpdatedOn,
				_user.Id
			);

			return result;
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}
}
