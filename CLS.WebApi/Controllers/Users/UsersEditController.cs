using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.Users;

[Route("api/users/[controller]")]
[Authorize(Roles = "Admin")]
[ApiController]
public class EditController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private readonly List<int> addedHierarchies = new();
	private UserObject? _user = new();

	public EditController(ApplicationDbContext context) => _context = context;

	[HttpGet("{id}")]
	public ActionResult<JsonResult> Get(int id) {
		var result = new UserIndexGetObject { data = new(), hierarchy = new(), roles = new() };
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.users, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var regions = _context.Hierarchy.Where(h => h.HierarchyLevel!.Id == 1).ToArray();
			result.hierarchy.Add(new() {
				hierarchy = regions.First().Name,
				id = regions.First().Id,
				sub = Helper.GetSubsLevel(_context, regions.First().Id),
				count = 0
			});

			var userRoles = _context.UserRole.OrderBy(u => u.Id);
			foreach (var role in userRoles) {
				result.roles.Add(new() { id = role.Id, name = role.Name });
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
						result.data.Add(currentUser);
					}
				}
				else {
					result.data.Add(currentUser);
				}
			}

			return new JsonResult(result);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpPost]
	public void Post([FromBody] string value) {
	}

	[HttpPut]
	public ActionResult<JsonResult> Put([FromBody] UserIndexDto value) {
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			var returnObject = new UserIndexGetObject { data = new() };
			var user = _context.User.Where(u => u.Id == value.id).First();
			if (value.userName != user.UserName) {
				if (_context.User.Where(u => u.UserName == value.userName).Any()) {
					throw new Exception(Resource.USERS_EXIST);
				}
			}

			var lastUpdatedOn = DateTime.Now;
			user.UserName = value.userName;
			user.LastName = value.lastName;
			user.FirstName = value.firstName;
			user.Department = value.department;
			user.Active = Helper.stringToBool(value.active);
			user.LastUpdatedOn = lastUpdatedOn;
			_context.Entry(user).Property("UserRoleId").CurrentValue = value.roleId;

			Helper.UserDeleteHierarchy(user.Id, _context);
			Helper.AddUserHierarchy(user.Id, _context, value.hierarchiesId, addedHierarchies);

			returnObject.data.Add(value);
			_context.SaveChanges();
			addedHierarchies.Clear();

			Helper.AddAuditTrail(_context,
				Resource.SECURITY,
				"SEC-04",
				"User Updated",
				@"ID=" + user.Id.ToString() + " / Username=" + user.UserName,
				lastUpdatedOn,
				_user.userId
			);

			if (user.Id == _user.userId) {
				//Helper.setUserTemp(user.UserName);
				var tempUser = Helper.GetUserObject(_context, _user.userName);
				if (tempUser != null) {
					if (Helper.userCookies.ContainsKey(tempUser.userId.ToString())) {
						Helper.userCookies.Remove(tempUser.userId.ToString());
					}

					Helper.userCookies.Add(tempUser.userId.ToString(), tempUser);
				}
			}

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
