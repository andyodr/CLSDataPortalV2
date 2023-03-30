using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.MeasureDefinition;

[ApiController]
[Route("api/measureDefinition/[controller]")]
[Authorize(Roles = "Regional Administrator, System Administrator")]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public FilterController(ApplicationDbContext context) => _dbc = context;

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
				MeasureTypes = _dbc.MeasureType.Select(m => new MeasureTypeFilterObject { Id = m.Id, Name = m.Name }).ToArray()
			};

			_user.savedFilters[Helper.pages.measureDefinition].measureTypeId ??= _dbc.MeasureType.FirstOrDefault()?.Id;
			result.Filter = _user.savedFilters[Helper.pages.measureDefinition];
			return result;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
