using Deliver.WebApi.Controllers.MeasureDefinition.Type;
using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Measures;

[ApiController]
[Route("api/measures/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public sealed class FilterController : BaseController
{
	[HttpGet("{measureTypeId?}/{hierarchyId?}")]
	public ActionResult<FilterReturnObject> GetAll(int? measureTypeId, int? hierarchyId) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			FilterReturnObject result = new() {
				MeasureTypes = Dbc.MeasureType
					.Select(m => new MeasureType(m.Id, m.Name, m.Description))
					.ToArray(),
				Hierarchy = new RegionFilterObject[] {
					Hierarchy.IndexController.CreateUserHierarchy(Dbc, _user.Id)
				}
			};

			var hId = _user.savedFilters[Pages.Measure].hierarchyId ??= 1;
			var tId = _user.savedFilters[Pages.Measure].measureTypeId ??= Dbc.MeasureType.First().Id;
			result.Filter = _user.savedFilters[Pages.Measure];
			result.Measures = IndexController.GetMeasures(Dbc, hierarchyId ?? hId, measureTypeId ?? tId);
			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}
}
