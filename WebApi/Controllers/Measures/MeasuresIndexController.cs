using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Measures;

[ApiController]
[Route("api/measures/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public class IndexController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public IndexController(ApplicationDbContext context) => _dbc = context;

	/// <summary>
	/// Gets hierarchy and measuredefinition data
	/// </summary>
	[HttpGet]
	public ActionResult<RegionIndexGetReturnObject> Get(int hierarchyId, int measureTypeId) {
		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			_user.savedFilters[pages.measure].hierarchyId = hierarchyId;
			_user.savedFilters[pages.measure].measureTypeId = measureTypeId;

			return GetMeasures(_dbc, hierarchyId, measureTypeId);
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}

	[NonAction]
	public static RegionIndexGetReturnObject GetMeasures(ApplicationDbContext dbc, int hierarchyId, int measureTypeId) {
		var hierarchies = dbc.Hierarchy
			.Where(h => h.Id == hierarchyId || h.HierarchyParentId == hierarchyId)
			.OrderBy(h => h.Id);
		var result = new RegionIndexGetReturnObject {
			Allow = true,
			Hierarchy = hierarchies.Select(h => h.Name).ToArray()
		};

		var measureDefinitions = from measureDef in dbc.MeasureDefinition
								 where measureDef.MeasureType!.Id == measureTypeId
								 orderby measureDef.FieldNumber ascending, measureDef.Name
								 select measureDef;

		foreach (var measuredef in measureDefinitions.AsNoTracking()) {
			var currentDataObject = new MeasureTypeRegionsObject { Hierarchy = new() };
			foreach (var hierarchy in hierarchies) {
				var measure = dbc.Measure
					.Where(m => m.MeasureDefinition!.Id == measuredef.Id && m.Hierarchy!.Id == hierarchy.Id)
					.AsNoTracking().ToArray();
				if (measure.Length > 0) {
					var newRegion = new RegionActiveCalculatedObject {
						Id = measure.First().Id,
						Active = measure.First().Active ?? false,
						Expression = measure.First().Expression ?? false,
						Rollup = measure.First().Rollup ?? false
					};
					currentDataObject.Id = measuredef.Id;
					currentDataObject.Name = measuredef.Name;
					currentDataObject.Owner = measure.FirstOrDefault()?.Owner ?? string.Empty;
					currentDataObject.Hierarchy.Add(newRegion);
				}
			}

			if (currentDataObject.Hierarchy.Count > 0) {
				result.Data.Add(currentDataObject);
			}
		}

		return result;
	}

	[HttpPut]
	public ActionResult<RegionIndexGetReturnObject> Put(MeasuresIndexPutObject dto) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		var returnObject = new RegionIndexGetReturnObject();
		var lastUpdatedOn = DateTime.Now;
		try {
			var currentMeasure = new MeasureTypeRegionsObject {
				Id = dto.MeasureDefinitionId,
				Hierarchy = new(),
				Name = _dbc.MeasureDefinition.Find(dto.MeasureDefinitionId)?.Name + "*"
			};

			foreach (var measureHierarchy in dto.Hierarchy) {
				var measure = _dbc.Measure.Where(m => m.Id == measureHierarchy.Id).FirstOrDefault();
				if (measure is not null) {
					bool updateMeasureData = measure.Active != measureHierarchy.Active ||
											 measure.Expression != measureHierarchy.Expression ||
											 measure.Rollup != measureHierarchy.Rollup;

					measure.Active = measureHierarchy.Active;
					measure.Expression = measureHierarchy.Expression;
					measure.Rollup = measureHierarchy.Rollup;
					measure.LastUpdatedOn = lastUpdatedOn;
					_dbc.SaveChanges();

					if (updateMeasureData) {
						var EnumIsProcessed = IsProcessed.measureData;
						if (!measure.Active ?? true) {
							EnumIsProcessed = IsProcessed.complete;
						}

						UpdateMeasureDataIsProcessed(_dbc, measure.Id, _user.Id, lastUpdatedOn, EnumIsProcessed);

						AddAuditTrail(_dbc,
							Resource.WEB_PAGES,
							"WEB-03",
							Resource.MEASURE,
							@"Updated / ID=" + measure.Id.ToString() +
									" / Active=" + measure.Active.ToString() +
									" / Expression=" + measure.Expression.ToString() +
									" / Rollup=" + measure.Rollup.ToString(),
							lastUpdatedOn,
							_user.Id
						);
					}
				}

				currentMeasure.Hierarchy.Add(measureHierarchy);
			}

			returnObject.Data.Add(currentMeasure);
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
