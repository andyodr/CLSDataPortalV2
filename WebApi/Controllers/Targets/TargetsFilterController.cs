using CLS.WebApi.Controllers.MeasureDefinition.Type;
using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers.Targets;

[ApiController]
[Route("api/targets/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;

	public FilterController(ApplicationDbContext context) => _dbc = context;

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
					Hierarchy.IndexController.CreateUserHierarchy(_dbc, _user.Id)
				}
			};

			result.MeasureTypes = _dbc.MeasureType
				.OrderBy(m => m.Id)
				.Select(m => new MeasureType(m.Id, m.Name, m.Description))
				.ToArray();

			_user.savedFilters[pages.target].measureTypeId ??= _dbc.MeasureType.First().Id;
			_user.savedFilters[pages.target].hierarchyId ??= 1;
			result.Filter = _user.savedFilters[pages.target];
			return result;
		}

		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
