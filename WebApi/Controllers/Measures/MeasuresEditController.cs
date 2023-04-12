using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers.Measures;

[ApiController]
[Route("api/measures/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public class EditController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public EditController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public ActionResult<MeasureIDReturnObject> Get([FromQuery] MeasuresOwnerObject values) {
		var returnObject = new MeasureIDReturnObject();

		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var measureDef = _context.MeasureDefinition
				.Include(d => d.MeasureType)
				.Where(md => md.Id == values.MeasureDefinitionId)
				.AsNoTrackingWithIdentityResolution().First();
			var data = new MeasureTypeDataObject {
				MeasureName = measureDef.Name,
				MeasureTypeName = measureDef.MeasureType!.Name
			};

			var hierarchies = from h in _context.Hierarchy
							  where h.HierarchyParentId == values.HierarchyId || h.Id == values.HierarchyId
							  orderby h.Id
							  select h;

			foreach (var hierarchy in hierarchies.AsNoTracking()) {
				var measure = _context.Measure
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
			return BadRequest(ErrorProcessing(_context, e, _user.Id));
		}
	}

	[HttpPut]
	public ActionResult<MeasureIDReturnObject> Put(MeasuresOwnerObject values) {
		var returnObject = new MeasureIDReturnObject();

		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var measureDef = _context.MeasureDefinition
				.Include(d => d.MeasureType)
				.Where(md => md.Id == values.MeasureDefinitionId)
				.First();
			var data = new MeasureTypeDataObject {
				MeasureName = measureDef.Name,
				MeasureTypeName = _context.MeasureType.Find(measureDef.MeasureTypeId)?.Name
			};

			var hierarchies = from h in _context.Hierarchy
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

				var measure = _context.Measure
					.Where(m => m.HierarchyId == hierarchy.Id && m.MeasureDefinition!.Id == values.MeasureDefinitionId)
					.FirstOrDefault();

				if (measure is not null) {
					measure.Owner = values.Owner;
					measure.LastUpdatedOn = lastUpdatedOn;
					any = true;
					UpdateMeasureDataIsProcessed(_context, measure.Id, _user.Id, lastUpdatedOn, IsProcessed.complete);

					AddAuditTrail(_context,
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
				_context.SaveChanges();
			}

			returnObject.Data.Add(data);
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_context, e, _user.Id));
		}
	}
}
