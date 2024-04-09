using Deliver.WebApi.Controllers.MeasureDefinition.Type;
using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.MeasureDefinition;

[ApiController]
[Route("api/measureDefinition/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public sealed class FilterController : BaseController
{
	[HttpGet]
	public ActionResult<FilterReturnObject> Get() {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			FilterReturnObject result = new() {
				MeasureTypes = [.. Dbc.MeasureType.Select(m => new MeasureType(m.Id, m.Name, m.Description))]
            };

			_user.savedFilters[Pages.MeasureDefinition].measureTypeId ??= Dbc.MeasureType.FirstOrDefault()?.Id;
			result.Filter = _user.savedFilters[Pages.MeasureDefinition];
			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}
}
