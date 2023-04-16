using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Users;

[ApiController]
[Route("api/users/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public class AddController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;

	public AddController(ApplicationDbContext context) => _dbc = context;

	/// <summary>
	/// Get hierarchy and role data from the database.
	/// </summary>
	/// <returns>An instance of UserIndexGetObject</returns>
	[HttpGet]
	public ActionResult<UserIndexGetObject> Get() {
		var returnObject = new UserIndexGetObject { Hierarchy = new(), Roles = new() };
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var region = _dbc.Hierarchy
				.Where(h => h.HierarchyLevel!.Id < 3).OrderBy(r => r.Id).AsNoTrackingWithIdentityResolution().First();
			returnObject.Hierarchy.Add(new() {
				Hierarchy = region.Name,
				Id = region.Id,
				Sub = GetSubsLevel(_dbc, region.Id),
				Count = 0
			});
			var userRoles = _dbc.UserRole.OrderBy(u => u.Id);
			foreach (var role in userRoles) {
				returnObject.Roles.Add(new() { Id = role.Id, Name = role.Name });
			}

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}

	/// <summary>
	/// Create a new user in the User table and return its userId.
	/// </summary>
	/// <param name="body"></param>
	/// <returns></returns>
	[HttpPost]
	public ActionResult<UserIndexGetObject> Post(UserIndexDto body) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			if (_dbc.User.Where(u => u.UserName == body.UserName).Any()) {
				return BadRequest(Resource.USERS_EXIST);
			}

			var lastUpdatedOn = DateTime.Now;

			var userEntry = _dbc.User.Add(new() {
				UserName = body.UserName,
				LastName = body.LastName,
				FirstName = body.FirstName,
				Department = body.Department,
				Active = body.Active,
				LastUpdatedOn = lastUpdatedOn
			});

			userEntry.Property("UserRoleId").CurrentValue = body.RoleId;
			var user = userEntry.Entity;
			_dbc.SaveChanges();
			body.Id = user.Id;
			if (body.HierarchiesId.Count > 0) {
				// Add all the child hierarchies first before inserting UserHierarchy
				var allSelectedHierarchies = _dbc.Hierarchy.FromSqlRaw($@"WITH f AS
(SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed
FROM Hierarchy WHERE Id IN ({string.Join(',', body.HierarchiesId)})
UNION ALL
SELECT h.Id, h.HierarchyLevelId, h.HierarchyParentId, h.[Name], h.Active, h.LastUpdatedOn, h.IsProcessed
FROM Hierarchy h JOIN f ON h.HierarchyParentId = f.Id
WHERE h.HierarchyLevelId > 3)
SELECT DISTINCT * FROM f").AsEnumerable().Select(h => h.Id).ToArray();
				foreach (var hId in allSelectedHierarchies) {
					_dbc.UserHierarchy.Add(new() { UserId = user.Id, HierarchyId = hId, LastUpdatedOn = lastUpdatedOn });
				}

				_dbc.SaveChanges();
			}

			AddAuditTrail(
			  _dbc, Resource.SECURITY,
			   "SEC-03",
			   "User Added",
			   @"ID=" + user.Id.ToString() + " / Username=" + user.UserName,
			   lastUpdatedOn,
			   _user.Id
			);

			return new UserIndexGetObject { Data = new UserIndexDto[] { body } };
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
