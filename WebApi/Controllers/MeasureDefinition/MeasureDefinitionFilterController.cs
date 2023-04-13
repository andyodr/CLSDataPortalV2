using CLS.WebApi.Controllers.MeasureDefinition.Type;
using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers.MeasureDefinition;

[ApiController]
[Route("api/measureDefinition/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public FilterController(ApplicationDbContext context) => _dbc = context;

	[HttpGet]
	public ActionResult<FilterReturnObject> Get() {
		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var result = new FilterReturnObject {
				MeasureTypes = _dbc.MeasureType.Select(m => new MeasureType(m.Id, m.Name, m.Description)).ToArray()
			};

			_user.savedFilters[pages.measureDefinition].measureTypeId ??= _dbc.MeasureType.FirstOrDefault()?.Id;
			result.Filter = _user.savedFilters[pages.measureDefinition];
			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}