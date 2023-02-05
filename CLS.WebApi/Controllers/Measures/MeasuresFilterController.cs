using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.Measures;

[Route("api/measures/[controller]")]
[Authorize]
[ApiController]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = new();

	public FilterController(ApplicationDbContext context) {
		_context = context;
	}

	[HttpGet]
	public ActionResult<JsonResult> GetAll() {
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.measure, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
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
				.ToList();
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

			if (_user.savedFilters[Helper.pages.measure].hierarchyId == null) {
				_user.savedFilters[Helper.pages.measure].hierarchyId = 1;
			}

			if (_user.savedFilters[Helper.pages.measure].measureTypeId == null) {
				_user.savedFilters[Helper.pages.measure].measureTypeId = _context.MeasureType.First().Id;
			}

			returnObject.filter = _user.savedFilters[Helper.pages.measure];
			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}
}
