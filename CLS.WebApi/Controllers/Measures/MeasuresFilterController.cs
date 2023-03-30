using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.Measures;

[ApiController]
[Route("api/measures/[controller]")]
[Authorize(Roles = "System Administrator")]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public FilterController(ApplicationDbContext context) => _dbc = context;

	[HttpGet]
	public ActionResult<FilterReturnObject> GetAll() {
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var result = new FilterReturnObject {
				MeasureTypes = _dbc.MeasureType.Select(m => new MeasureTypeFilterObject {
					Id = m.Id,
					Name = m.Name,
					Description = m.Description
				}).ToArray(),
				Hierarchy = new() { Hierarchy.IndexController.CreateUserHierarchy(_dbc, _user.Id) }
			};

			var hierarchyId = _user.savedFilters[Helper.pages.measure].hierarchyId ??= 1;
			var measureTypeId = _user.savedFilters[Helper.pages.measure].measureTypeId ??= _dbc.MeasureType.First().Id;
			result.Filter = _user.savedFilters[Helper.pages.measure];
			result.Measures = IndexController.GetMeasures(_dbc, hierarchyId, measureTypeId);
			return result;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
