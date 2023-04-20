using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Measures;

[ApiController]
[Route("api/measures/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public sealed class EditController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;

	public EditController(ApplicationDbContext context) => _dbc = context;

	[HttpGet]
	public ActionResult<MeasureIDReturnObject> Get([FromQuery] MeasuresOwnerObject values) {
		var returnObject = new MeasureIDReturnObject();
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var measureDef = _dbc.MeasureDefinition
				.Include(d => d.MeasureType)
				.Where(md => md.Id == values.MeasureDefinitionId)
				.AsNoTrackingWithIdentityResolution().First();
			var data = new MeasureTypeDataObject {
				MeasureName = measureDef.Name,
				MeasureTypeName = measureDef.MeasureType!.Name
			};

			var hierarchies = from h in _dbc.Hierarchy
							  where h.HierarchyParentId == values.HierarchyId || h.Id == values.HierarchyId
							  orderby h.Id
							  select h;

			foreach (var hierarchy in hierarchies.AsNoTracking()) {
				var measure = _dbc.Measure
							  .Where(m => m.HierarchyId == hierarchy.Id && m.MeasureDefinitionId == values.MeasureDefinitionId)
							  .AsNoTrackingWithIdentityResolution().First();

				var hierarchyOwner = new RegionOwnerObject {
					Id = hierarchy.Id,
					Name = hierarchy.Name
				};
				data.Hierarchy.Add(hierarchyOwner);
				data.Owner = measure.Owner;
			}

			returnObject.Data.Add(data);
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}

	[HttpPut]
	public ActionResult<MeasureIDReturnObject> Put(MeasuresOwnerObject values) {
		var returnObject = new MeasureIDReturnObject();
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var measureDef = _dbc.MeasureDefinition
				.Include(d => d.MeasureType)
				.Where(md => md.Id == values.MeasureDefinitionId)
				.First();
			var data = new MeasureTypeDataObject {
				MeasureName = measureDef.Name,
				MeasureTypeName = _dbc.MeasureType.Find(measureDef.MeasureTypeId)?.Name
			};

			var hierarchies = from h in _dbc.Hierarchy
							  where h.HierarchyParentId == values.HierarchyId || h.Id == values.HierarchyId
							  orderby h.Id
							  select h;

			bool any = false;
			data.Hierarchy = new List<RegionOwnerObject>();
			data.Owner = values.Owner;
			var lastUpdatedOn = DateTime.Now;
			foreach (var hierarchy in hierarchies) {
				data.Hierarchy.Add(new() {
					Id = hierarchy.Id,
					Name = hierarchy.Name
				});

				var measure = _dbc.Measure
					.Where(m => m.HierarchyId == hierarchy.Id && m.MeasureDefinition!.Id == values.MeasureDefinitionId)
					.FirstOrDefault();

				if (measure is not null) {
					measure.Owner = values.Owner;
					measure.LastUpdatedOn = lastUpdatedOn;
					any = true;
					UpdateMeasureDataIsProcessed(_dbc, measure.Id, _user.Id, lastUpdatedOn, IsProcessed.Complete);

					AddAuditTrail(_dbc,
						Resource.WEB_PAGES,
						"WEB-03",
						Resource.MEASURE,
						@"Updated Owner / ID=" + measure.Id.ToString() +
								" / Owner=" + measure.Owner,
						lastUpdatedOn,
						_user.Id
					);
				}
			}

			if (any) {
				_dbc.SaveChanges();
			}

			returnObject.Data.Add(data);
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
