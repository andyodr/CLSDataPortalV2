using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.Settings;

[ApiController]
[Route("api/settings/[controller]")]
[Authorize(Roles = "System Administrator")]
public class RecalculateController : Controller
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public RecalculateController(ApplicationDbContext context) => _context = context;

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
	public ActionResult Put([FromBody] dynamic jsonString) {
		try {
			if (Helper.UserAuthorization(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			return Ok();
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
