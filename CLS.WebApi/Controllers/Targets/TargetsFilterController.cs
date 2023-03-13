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

	/// <summary>
	/// Get measureType and hierarchy data
	/// </summary>
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

			var measureTypes = _context.MeasureType
				.OrderBy(m => m.Id)
				.Select(m => new MeasureTypeFilterObject { Id = m.Id, Name = m.Name, Description = m.Description })
				.AsNoTracking();
			foreach (var mtfo in measureTypes) {
				filter.measureTypes.Add(mtfo);
			}

			var regions = _context.Hierarchy
				.Where(m => m.HierarchyLevel!.Id == 1)
				.OrderBy(r => r.Id)
				.AsNoTrackingWithIdentityResolution()
				.ToArray();
			filter.hierarchy.Add(new() {
				Hierarchy = regions.First().Name,
				Id = regions.First().Id,
				Sub = Helper.GetSubs(_context, regions.First().Id, _user),
				Count = 0
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
