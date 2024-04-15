using Deliver.WebApi.Controllers.MeasureDefinition.Type;
using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
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
	public ActionResult<FilterResponse> Get() {
		if (CreateUserObject(User) is not UserDto _user) {
			return Unauthorized();
		}

		try {
			FilterResponse result = new() {
				MeasureTypes = [.. Dbc.MeasureType.Select(m => new MeasureType(m.Id, m.Name, m.Description))]
            };

			_user.savedFilters[Pages.MeasureDefinition].measureTypeId ??= Dbc.MeasureType.FirstOrDefault()?.Id;
			result.Filter = _user.savedFilters[Pages.MeasureDefinition];
			return result;
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}
}
