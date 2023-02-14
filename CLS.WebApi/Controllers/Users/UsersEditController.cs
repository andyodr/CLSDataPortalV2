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
	private readonly List<int> addedHierarchies = new();
	private UserObject _user = null!;

	public EditController(ApplicationDbContext context) => _context = context;

	/// <summary>
	/// Get hierarchy and role data, and user data for a subset of users.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	[HttpGet("{id}")]
	public ActionResult<UserIndexGetObject> Get(int id) {
		var result = new UserIndexGetObject { data = new(), hierarchy = new(), roles = new() };
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
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
	/// <param name="value"></param>
	/// <returns></returns>
	[HttpPut]
	public ActionResult<UserIndexGetObject> Put(UserIndexDto value) {
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var returnObject = new UserIndexGetObject { data = new() };
			var user = _context.User.Find(value.id);
			if (user == null) {
				return ValidationProblem("User ID not found.");
			}

			if (value.userName != user.UserName) {
				if (_context.User.Where(u => u.UserName == value.userName).Any()) {
					return ValidationProblem(Resource.USERS_EXIST);
				}
			}

			var lastUpdatedOn = DateTime.Now;
			user.UserName = value.userName;
			user.LastName = value.lastName;
			user.FirstName = value.firstName;
			user.Department = value.department;
			user.Active = Helper.StringToBool(value.active);
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

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}
}
