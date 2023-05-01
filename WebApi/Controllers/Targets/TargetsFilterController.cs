using Deliver.WebApi.Controllers.MeasureDefinition.Type;
using Deliver.WebApi.Data;
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
	public ActionResult<FilterReturnObject> Get() {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var result = new FilterReturnObject {
				Intervals = null,
				MeasureTypes = new List<MeasureType>(),
				Hierarchy = new RegionFilterObject[] {
					Hierarchy.IndexController.CreateUserHierarchy(Dbc, _user.Id)
				}
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
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}
}
