using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.Targets;

[Route("api/targets/[controller]")]
[Authorize]
[ApiController]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject? _user = new();

	public FilterController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public ActionResult<JsonResult> Get() {
		var filter = new FilterReturnObject {
			intervals = null,
			measureTypes = new List<MeasureTypeFilterObject>(),
			hierarchy = new List<RegionFilterObject>()
		};

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.target, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var measureTypes = _context.MeasureType.OrderBy(m => m.Id);
			foreach (var measureType in measureTypes.AsNoTracking()) {
				filter.measureTypes.Add(new() { Id = measureType.Id, Name = measureType.Name });
			}

			var regions = _context.Hierarchy.Where(m => m.HierarchyLevel!.Id == 1).OrderBy(r => r.Id).ToList();
			filter.hierarchy.Add(new() {
				hierarchy = regions.First().Name,
				id = regions.First().Id,
				sub = Helper.GetSubs(_context, regions.First().Id, _user),
				count = 0
			});

			_user.savedFilters[Helper.pages.target].measureTypeId ??= _context.MeasureType.First().Id;
			_user.savedFilters[Helper.pages.target].hierarchyId ??= 1;
			filter.filter = _user.savedFilters[Helper.pages.target];

			return new JsonResult(filter);
		}

		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}
}
