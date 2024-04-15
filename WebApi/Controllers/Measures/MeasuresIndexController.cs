using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Measures;

[ApiController]
[Route("api/measures/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public sealed class IndexController : BaseController
{
	/// <summary>
	/// Gets hierarchy and measuredefinition data
	/// </summary>
	[HttpGet]
	public ActionResult<RegionIndexGetResponse> Get(int hierarchyId, int measureTypeId) {
		if (CreateUserObject(User) is not UserDto _user) {
			return Unauthorized();
		}

		try {
			_user.savedFilters[Pages.Measure].hierarchyId = hierarchyId;
			_user.savedFilters[Pages.Measure].measureTypeId = measureTypeId;

			return GetMeasures(Dbc, hierarchyId, measureTypeId);
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}

	[NonAction]
	public static RegionIndexGetResponse GetMeasures(ApplicationDbContext dbc, int hierarchyId, int measureTypeId) {
		Data.Models.Hierarchy[] hierarchies = [..dbc.Hierarchy
			.Where(h => h.Id == hierarchyId || h.HierarchyParentId == hierarchyId && h.Active == true)
			.OrderBy(h => h.HierarchyParentId)
			.ThenBy(h => h.Id).AsNoTrackingWithIdentityResolution()];
		RegionIndexGetResponse result = new() {
			Allow = true,
			Hierarchy = [.. hierarchies.Select(h => h.Name)]
		};

		var measureDefinitions = from measureDef in dbc.MeasureDefinition
								 where measureDef.MeasureType!.Id == measureTypeId
								 orderby measureDef.FieldNumber ascending, measureDef.Name
								 select measureDef;

		foreach (var measuredef in measureDefinitions.AsNoTrackingWithIdentityResolution()) {
			MeasureTypeRegionsObject currentDataObject = new() { Hierarchy = [] };
			foreach (var hierarchy in hierarchies) {
				var measure = dbc.Measure
					.Where(m => m.MeasureDefinition!.Id == measuredef.Id && m.Hierarchy!.Id == hierarchy.Id)
					.AsNoTrackingWithIdentityResolution().ToArray();
				if (measure.Length > 0) {
					RegionActiveCalculatedObject newRegion = new() {
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
	public ActionResult<RegionIndexGetResponse> Put(MeasuresIndexRequest dto) {
		if (CreateUserObject(User) is not UserDto _user) {
			return Unauthorized();
		}

		var lastUpdatedOn = DateTime.Now;
		try {
			var currentMeasure = new MeasureTypeRegionsObject {
				Id = dto.MeasureDefinitionId,
				Hierarchy = new(),
				Name = Dbc.MeasureDefinition.Find(dto.MeasureDefinitionId)?.Name + "*"
			};

			foreach (var measureHierarchy in dto.Hierarchy) {
				var measure = Dbc.Measure.Where(m => m.Id == measureHierarchy.Id).FirstOrDefault();
				if (measure is not null) {
					bool updateMeasureData = measure.Active != measureHierarchy.Active ||
											 measure.Expression != measureHierarchy.Expression ||
											 measure.Rollup != measureHierarchy.Rollup;

					measure.Active = measureHierarchy.Active;
					measure.Expression = measureHierarchy.Expression;
					measure.Rollup = measureHierarchy.Rollup;
					measure.LastUpdatedOn = lastUpdatedOn;
					Dbc.SaveChanges();

					if (updateMeasureData) {
						var EnumIsProcessed = IsProcessed.MeasureData;
						if (!measure.Active ?? true) {
							EnumIsProcessed = IsProcessed.Complete;
						}

						Dbc.UpdateMeasureDataIsProcessed(measure.Id, _user.Id, lastUpdatedOn, EnumIsProcessed);

						Dbc.AddAuditTrail(Resource.WEB_PAGES, "WEB-03",
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

			RegionIndexGetResponse returnObject = new() { Data = [currentMeasure] };
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}
}
