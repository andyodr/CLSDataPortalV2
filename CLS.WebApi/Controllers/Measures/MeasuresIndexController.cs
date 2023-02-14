using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.Measures;

[ApiController]
[Route("api/measures/[controller]")]
[Authorize(Roles = "System Administrator")]
public class IndexController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public IndexController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public ActionResult<RegionIndexGetReturnObject> Get(MeasuresIndexGetRecieveObject values) {
		var returnObject = new RegionIndexGetReturnObject {
			hierarchy = new(),
			data = new()
		};

		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
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
									 where measureDef.MeasureType!.Id == values.measureTypeId
									 orderby measureDef.FieldNumber ascending, measureDef.Name
									 select measureDef;

			foreach (var measuredef in measureDefinitions.AsNoTracking()) {
				var currentDataObject = new MeasureTypeRegionsObject { hierarchy = new() };
				foreach (var hierarchy in hierarchies) {
					var measure = _context.Measure
						.Where(m => m.MeasureDefinition!.Id == measuredef.Id && m.Hierarchy!.Id == hierarchy.Id)
						.AsNoTracking().ToArray();
					if (measure.Length > 0) {
						var newRegion = new RegionActiveCalculatedObject {
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

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpPut]
	public ActionResult<RegionIndexGetReturnObject> Put(MeasuresIndexPutObject value) {
		if (Helper.CreateUserObject(User) is UserObject u) {
			_user = u;
		}
		else {
			return Unauthorized();
		}

		var returnObject = new RegionIndexGetReturnObject { data = new() };
		var lastUpdatedOn = DateTime.Now;
		try {
			var currentMeasure = new MeasureTypeRegionsObject {
				id = value.measureDefinitionId,
				hierarchy = new(),
				name = _context.MeasureDefinition.Find(value.measureDefinitionId)?.Name
			};

			foreach (var measureHierarchy in value.hierarchy) {
				var measure = _context.Measure.Where(m => m.Id == measureHierarchy.id).FirstOrDefault();
				if (measure is not null) {
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

						Helper.UpdateMeasureDataIsProcessed(_context, measure.Id, _user.userId, lastUpdatedOn, EnumIsProcessed);

						Helper.AddAuditTrail(_context,
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
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}
}
