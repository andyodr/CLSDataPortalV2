using Deliver.WebApi.Controllers.MeasureDefinition.Type;
using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Measures;

[ApiController]
[Route("api/measures/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;

	public FilterController(ApplicationDbContext context) => _dbc = context;

	[HttpGet("{measureTypeId?}/{hierarchyId?}")]
	public ActionResult<FilterReturnObject> GetAll(int? measureTypeId, int? hierarchyId) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			FilterReturnObject result = new() {
				MeasureTypes = _dbc.MeasureType
					.Select(m => new MeasureType(m.Id, m.Name, m.Description))
					.ToArray(),
				Hierarchy = new RegionFilterObject[] {
					Hierarchy.IndexController.CreateUserHierarchy(_dbc, _user.Id)
				}
			};

			var hId = _user.savedFilters[Pages.Measure].hierarchyId ??= 1;
			var tId = _user.savedFilters[Pages.Measure].measureTypeId ??= _dbc.MeasureType.First().Id;
			result.Filter = _user.savedFilters[Pages.Measure];
			result.Measures = IndexController.GetMeasures(_dbc, hierarchyId ?? hId, measureTypeId ?? tId);
			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
