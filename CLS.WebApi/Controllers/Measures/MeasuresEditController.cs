using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.Measures;

[ApiController]
[Route("api/measures/[controller]")]
[Authorize(Roles = "System Administrator")]
public class EditController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public EditController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public ActionResult<MeasureIDReturnObject> Get(MeasuresOwnerObject values) {
		var returnObject = new MeasureIDReturnObject();

		try {
			if (Helper.UserAuthorization(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var measureDef = _context.MeasureDefinition
				.Include(d => d.MeasureType)
				.Where(md => md.Id == values.measureDefinitionId)
				.AsNoTrackingWithIdentityResolution().First();
			var data = new MeasureTypeDataObject {
				MeasureName = measureDef.Name,
				MeasureTypeName = measureDef.MeasureType!.Name
			};

			var hierarchies = from h in _context.Hierarchy
							  where h.HierarchyParentId == values.hierarchyId || h.Id == values.hierarchyId
							  orderby h.Id
							  select h;

			foreach (var hierarchy in hierarchies.AsNoTracking()) {
				var measure = _context.Measure
							  .Where(m => m.HierarchyId == hierarchy.Id && m.MeasureDefinitionId == values.measureDefinitionId)
							  .AsNoTrackingWithIdentityResolution().First();

				var hierarchyOwner = new RegionOwnerObject {
					id = hierarchy.Id,
					name = hierarchy.Name
				};
				data.Hierarchy.Add(hierarchyOwner);
				data.Owner = measure.Owner;
			}

			returnObject.data.Add(data);
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpPut]
	[ProducesResponseType(StatusCodes.Status202Accepted)]
	public ActionResult<MeasureIDReturnObject> Put([FromBody] MeasuresOwnerObject values) {
		var returnObject = new MeasureIDReturnObject();

		try {
			if (Helper.UserAuthorization(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var measureDef = _context.MeasureDefinition
				.Include(d => d.MeasureType)
				.Where(md => md.Id == values.measureDefinitionId)
				.First();
			var data = new MeasureTypeDataObject {
				MeasureName = measureDef.Name,
				MeasureTypeName = _context.MeasureType.Find(measureDef.MeasureTypeId)?.Name
			};

			var hierarchies = from h in _context.Hierarchy
							  where h.HierarchyParentId == values.hierarchyId || h.Id == values.hierarchyId
							  orderby h.Id
							  select h;

			bool any = false;
			data.Hierarchy = new List<RegionOwnerObject>();
			data.Owner = values.owner;
			var lastUpdatedOn = DateTime.Now;
			foreach (var hierarchy in hierarchies) {
				data.Hierarchy.Add(new() {
					id = hierarchy.Id,
					name = hierarchy.Name
				});

				var measure = _context.Measure
							  .Where(m => m.HierarchyId == hierarchy.Id && m.MeasureDefinition!.Id == values.measureDefinitionId).FirstOrDefault();

				if (measure is not null) {
					measure.Owner = values.owner;
					measure.LastUpdatedOn = lastUpdatedOn;
					any = true;
					Helper.UpdateMeasureDataIsProcessed(_context, measure.Id, _user.userId, lastUpdatedOn, Helper.IsProcessed.complete);

					Helper.AddAuditTrail(_context,
						Resource.WEB_PAGES,
						"WEB-03",
						Resource.MEASURE,
						@"Updated Owner / ID=" + measure.Id.ToString() +
								" / Owner=" + measure.Owner,
						lastUpdatedOn,
						_user.userId
					);
				}
			}

			if (any) {
				_context.SaveChanges();
			}

			returnObject.data.Add(data);
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}
}
