using Deliver.WebApi.Controllers.MeasureDefinition.Type;
using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Targets;

[ApiController]
[Route("api/targets/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public sealed class FilterController : BaseController
{
	/// <summary>
	/// Get measureType and hierarchy data
	/// </summary>
	[HttpGet]
	public ActionResult<FilterResponse> Get() {
		if (CreateUserObject(User) is not UserDto _user) {
			return Unauthorized();
		}

		try {
			var result = new FilterResponse {
				Intervals = null,
				MeasureTypes = [],
				Hierarchy = [Hierarchy.IndexController.CreateUserHierarchy(Dbc, _user.Id)]
			};

			result.MeasureTypes = Dbc.MeasureType
				.OrderBy(m => m.Id)
				.Select(m => new MeasureType(m.Id, m.Name, m.Description))
				.ToArray();

			_user.savedFilters[Pages.Target].measureTypeId ??= Dbc.MeasureType.First().Id;
			_user.savedFilters[Pages.Target].hierarchyId ??= 1;
			result.Filter = _user.savedFilters[Pages.Target];
			return result;
		}

		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}
}
