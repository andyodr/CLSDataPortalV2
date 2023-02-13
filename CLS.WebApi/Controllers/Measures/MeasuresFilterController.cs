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
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public FilterController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public ActionResult<FilterReturnObject> GetAll() {
		try {
			if (Helper.UserAuthorization(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var returnObject = new FilterReturnObject { measureTypes = new(), hierarchy = new() };
			foreach (var metricType in _context.MeasureType) {
				returnObject.measureTypes.Add(new() {
					Id = metricType.Id,
					Name = metricType.Name,
					Description = metricType.Description
				});
			}

			var regionList = _context.Hierarchy.Where(m => m.HierarchyLevel!.Id == 1)
				.OrderBy(r => r.Id)
				.AsNoTrackingWithIdentityResolution()
				.ToArray();
			foreach (var region in regionList) {
				var subs = Helper.GetSubs(_context, region.Id, _user);
				if (subs.Count > 0) {
					returnObject.hierarchy.Add(new() {
						hierarchy = region.Name,
						id = region.Id,
						sub = subs,
						count = subs.Count
					});
				}
			}

			_user.savedFilters[Helper.pages.measure].hierarchyId ??= 1;
			_user.savedFilters[Helper.pages.measure].measureTypeId ??= _context.MeasureType.First().Id;
			returnObject.filter = _user.savedFilters[Helper.pages.measure];
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}
}
