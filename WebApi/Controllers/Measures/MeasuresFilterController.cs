using CLS.WebApi.Controllers.MeasureDefinition.Type;
using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers.Measures;

[ApiController]
[Route("api/measures/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public FilterController(ApplicationDbContext context) => _dbc = context;

	[HttpGet]
	public ActionResult<FilterReturnObject> GetAll() {
		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var result = new FilterReturnObject {
				MeasureTypes = _dbc.MeasureType
					.Select(m => new MeasureType(m.Id, m.Name, m.Description))
					.ToArray(),
				Hierarchy = new RegionFilterObject[] {
					Hierarchy.IndexController.CreateUserHierarchy(_dbc, _user.Id)
				}
			};

			var hierarchyId = _user.savedFilters[pages.measure].hierarchyId ??= 1;
			var measureTypeId = _user.savedFilters[pages.measure].measureTypeId ??= _dbc.MeasureType.First().Id;
			result.Filter = _user.savedFilters[pages.measure];
			result.Measures = IndexController.GetMeasures(_dbc, hierarchyId, measureTypeId);
			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
