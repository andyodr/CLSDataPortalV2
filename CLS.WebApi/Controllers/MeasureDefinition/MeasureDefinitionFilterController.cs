using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.MeasureDefinition;

[Route("api/measureDefinition/[controller]")]
[Authorize]
[ApiController]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = new();

	public FilterController(ApplicationDbContext context) {
		_context = context;
	}

	// GET: api/values
	[HttpGet]
	public ActionResult<JsonResult> Get() {
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.measureDefinition, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			FilterReturnObject returnObject = new() { measureTypes = new() };
			var measureTypes = _context.MeasureType;
			foreach (var measuretype in measureTypes.AsNoTracking()) {
				returnObject.measureTypes.Add(new MeasureTypeFilterObject { Id = measuretype.Id, Name = measuretype.Name });
			}

			_user.savedFilters[Helper.pages.measureDefinition].measureTypeId ??= _context.MeasureType.FirstOrDefault()?.Id;
			returnObject.filter = _user.savedFilters[Helper.pages.measureDefinition];
			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	// GET api/values/5

	// POST api/values
	[HttpPost]
	public void Post([FromBody] string value) {
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
