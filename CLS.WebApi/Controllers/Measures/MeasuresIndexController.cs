using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.Measures;

[Route("api/measures/[controller]")]
[Authorize]
[ApiController]
public class IndexController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = new();

	public IndexController(ApplicationDbContext context) {
		_context = context;
	}

	[HttpGet]
	public ActionResult<JsonResult> Get(MeasuresIndexGetRecieveObject values) {
		RegionIndexGetReturnObject returnObject = new() {
			hierarchy = new List<string>(),
			data = new List<MeasureTypeRegionsObject>()
		};

		try {

			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.measure, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			//returnObject.allow = _user.hierarchyIds.Contains(values.hierarchyId);
			returnObject.allow = true;

			var hierarchies = from hierarchy in _context.Hierarchy
							  where hierarchy.HierarchyParentId == values.hierarchyId || hierarchy.Id == values.hierarchyId
							  orderby hierarchy.Id
							  select hierarchy;

			foreach (var hierarchy in hierarchies) {
				returnObject.hierarchy.Add(hierarchy.Name);
			}

			var measureDefinitions = from measureDef in _context.MeasureDefinition
									 where measureDef.MeasureType.Id == values.measureTypeId
									 orderby measureDef.FieldNumber ascending, measureDef.Name
									 select measureDef;

			foreach (var measuredef in measureDefinitions.AsNoTracking()) {
				MeasureTypeRegionsObject currentDataObject = new() { hierarchy = new() };
				foreach (var hierarchy in hierarchies) {
					var measure = _context.Measure
						.Where(m => m.MeasureDefinition!.Id == measuredef.Id && m.Hierarchy.Id == hierarchy.Id)
						.AsNoTracking().ToList();
					if (measure.Count > 0) {
						RegionActiveCalculatedObject newRegion = new() {
							id = measure.First().Id,
							active = measure.First().Active ?? false,
							expression = measure.First().Expression ?? false,
							rollup = measure.First().Rollup ?? false
						};
						currentDataObject.id = measuredef.Id;
						currentDataObject.name = measuredef.Name;
						currentDataObject.owner = measure.FirstOrDefault()?.Owner ?? string.Empty;
						currentDataObject.hierarchy.Add(newRegion);
					}
				}

				if (currentDataObject.hierarchy.Count > 0) {
					returnObject.data.Add(currentDataObject);
				}
			}

			_user.savedFilters[Helper.pages.measure].hierarchyId = values.hierarchyId;
			_user.savedFilters[Helper.pages.measure].measureTypeId = values.measureTypeId;

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpPut]
	public ActionResult<JsonResult> Put([FromBody] MeasuresIndexPutObject value) {
		_user = Helper.UserAuthorization(User);
		if (_user == null) {
			throw new Exception();
		}

		if (!Helper.IsUserPageAuthorized(Helper.pages.measure, _user.userRoleId)) {
			throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
		}

		RegionIndexGetReturnObject returnObject = new() { data = new() };
		var lastUpdatedOn = DateTime.Now;
		try {
			MeasureTypeRegionsObject currentMeasure = new() {
				id = value.measureDefinitionId,
				hierarchy = new List<RegionActiveCalculatedObject>(),
				name = _context.MeasureDefinition.Find(value.measureDefinitionId)?.Name
			};

			foreach (var measureHierarchy in value.hierarchy) {
				var measure = _context.Measure.Where(m => m.Id == measureHierarchy.id).FirstOrDefault();
				if (measure != null) {
					bool updateMeasureData = measure.Active != measureHierarchy.active ||
											 measure.Expression != measureHierarchy.expression ||
											 measure.Rollup != measureHierarchy.rollup;

					measure.Active = measureHierarchy.active;
					measure.Expression = measureHierarchy.expression;
					measure.Rollup = measureHierarchy.rollup;
					measure.LastUpdatedOn = lastUpdatedOn;
					_context.SaveChanges();

					if (updateMeasureData) {
						var EnumIsProcessed = Helper.IsProcessed.measureData;
						if (!measure.Active ?? true) {
							EnumIsProcessed = Helper.IsProcessed.complete;
						}

						Helper.UpdateMeasureDataIsProcessed(measure.Id, _user.userId, lastUpdatedOn, EnumIsProcessed);

						Helper.addAuditTrail(
						  Resource.WEB_PAGES,
						   "WEB-03",
						   Resource.MEASURE,
						   @"Updated / ID=" + measure.Id.ToString() +
								   " / Active=" + measure.Active.ToString() +
								   " / Expression=" + measure.Expression.ToString() +
								   " / Rollup=" + measure.Rollup.ToString(),
						   lastUpdatedOn,
						   _user.userId
						);
					}
				}

				currentMeasure.hierarchy.Add(measureHierarchy);
			}

			returnObject.data.Add(currentMeasure);
			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}
}
