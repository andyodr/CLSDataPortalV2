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
	private List<int> addedHierarchies = new();
	private UserObject _user = new();

	public AddController(ApplicationDbContext context) {
		_context = context;
	}

	// GET: api/values
	[HttpGet]
	public ActionResult<JsonResult> Get() {
		UserIndexGetObject returnObject = new() { hierarchy = new(), roles = new() };
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.users, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var regions = _context.Hierarchy.Where(h => h.HierarchyLevel!.Id < 3).OrderBy(r => r.Id).ToList();
			returnObject.hierarchy.Add(new RegionFilterObject { hierarchy = regions.ElementAt(0).Name, id = regions.ElementAt(0).Id, sub = Helper.getSubsLevel(regions.ElementAt(0).Id), count = 0 });
			var userRoles = _context.UserRole.OrderBy(u => u.Id);
			foreach (var role in userRoles) {
				returnObject.roles.Add(new IntervalsObject { id = role.Id, name = role.Name });
			}

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	// GET api/values/5
	[HttpGet("{id}")]
	public string Get(int id) {
		return "value";
	}

	// POST api/values
	[HttpPost]
	public ActionResult<JsonResult> Post([FromBody] UserIndexDto value) {
		UserIndexGetObject returnObject = new() { data = new() };
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (_context.User.Where(u => u.UserName == value.userName).Count() > 0)
				throw new Exception(Resource.USERS_EXIST);

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

			Helper.addAuditTrail(
			  Resource.SECURITY,
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

	// PUT api/values/5
	[HttpPut("{id}")]
	public void Put(int id, [FromBody] string value) {
	}

	// DELETE api/values/5
	[HttpDelete("{id}")]
	public void Delete(int id) {
	}

}
