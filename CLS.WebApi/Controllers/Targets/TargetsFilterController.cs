using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.Targets;

[ApiController]
[Route("api/targets/[controller]")]
[Authorize(Roles = "Regional Administrator, System Administrator")]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public FilterController(ApplicationDbContext context) => _dbc = context;

	/// <summary>
	/// Get measureType and hierarchy data
	/// </summary>
	[HttpGet]
	public ActionResult<FilterReturnObject> Get() {
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var result = new FilterReturnObject {
				Intervals = null,
				MeasureTypes = new List<MeasureTypeFilterObject>(),
				Hierarchy = new() { Hierarchy.IndexController.CreateUserHierarchy(_dbc, _user.Id) }
			};

			result.MeasureTypes = _dbc.MeasureType
				.OrderBy(m => m.Id)
				.Select(m => new MeasureTypeFilterObject { Id = m.Id, Name = m.Name, Description = m.Description })
				.ToArray();

			_user.savedFilters[Helper.pages.target].measureTypeId ??= _dbc.MeasureType.First().Id;
			_user.savedFilters[Helper.pages.target].hierarchyId ??= 1;
			result.Filter = _user.savedFilters[Helper.pages.target];
			return result;
		}

		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
