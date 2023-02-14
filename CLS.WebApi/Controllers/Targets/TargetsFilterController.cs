using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.Targets;

[ApiController]
[Route("api/targets/[controller]")]
[Authorize(Roles = "Regional Administrator, System Administrator")]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public FilterController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public ActionResult<FilterReturnObject> Get() {
		var filter = new FilterReturnObject {
			intervals = null,
			measureTypes = new List<MeasureTypeFilterObject>(),
			hierarchy = new List<RegionFilterObject>()
		};

		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var measureTypes = _context.MeasureType.OrderBy(m => m.Id);
			foreach (var measureType in measureTypes.AsNoTracking()) {
				filter.measureTypes.Add(new() { Id = measureType.Id, Name = measureType.Name });
			}

			var regions = _context.Hierarchy.Where(m => m.HierarchyLevel!.Id == 1).OrderBy(r => r.Id).ToArray();
			filter.hierarchy.Add(new() {
				hierarchy = regions.First().Name,
				id = regions.First().Id,
				sub = Helper.GetSubs(_context, regions.First().Id, _user),
				count = 0
			});

			_user.savedFilters[Helper.pages.target].measureTypeId ??= _context.MeasureType.First().Id;
			_user.savedFilters[Helper.pages.target].hierarchyId ??= 1;
			filter.filter = _user.savedFilters[Helper.pages.target];

			return filter;
		}

		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}
}
