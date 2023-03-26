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
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public FilterController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public ActionResult<FilterReturnObject> Get() {
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var returnObject = new FilterReturnObject { MeasureTypes = new() };
			foreach (var measuretype in _context.MeasureType.AsNoTrackingWithIdentityResolution()) {
				returnObject.MeasureTypes.Add(new MeasureTypeFilterObject { Id = measuretype.Id, Name = measuretype.Name });
			}

			_user.savedFilters[Helper.pages.measureDefinition].measureTypeId ??= _context.MeasureType.FirstOrDefault()?.Id;
			returnObject.Filter = _user.savedFilters[Helper.pages.measureDefinition];
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.Id));
		}
	}
}
