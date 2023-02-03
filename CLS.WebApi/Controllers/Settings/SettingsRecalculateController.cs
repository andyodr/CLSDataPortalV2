using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.Settings;

[Route("api/settings/[controller]")]
[Authorize]
[ApiController]
public class RecalculateController : Controller
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = new();

	public RecalculateController(ApplicationDbContext context) {
		_context = context;
	}

	[HttpGet]
	public IEnumerable<string> Get() {
		return new string[] { "value1", "value2" };
	}

	[HttpGet("{id}")]
	public string Get(int id) {
		return "value";
	}

	[HttpPost]
	public void Post([FromBody] string value) {
	}

	[HttpPut]
	public ActionResult<JsonResult> Put([FromBody] dynamic jsonString) {

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.settings, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			return new JsonResult(null);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
