using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.Measures;

[Route("api/measures/[controller]")]
[Authorize]
[ApiController]
public class EditController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject? _user = new();

	public EditController(ApplicationDbContext context) {
		_context = context;
	}

	[HttpGet]
	public ActionResult<JsonResult> Get(MeasuresOwnerObject values) {
		var returnObject = new MeasureIDReturnObject { data = new() };

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.measure, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var measureDef = _context.MeasureDefinition
				.Include(d => d.MeasureType)
				.Where(md => md.Id == values.measureDefinitionId)
				.AsNoTrackingWithIdentityResolution().First();
			var data = new MeasureTypeDataObject {
				measureName = measureDef.Name,
				measureTypeName = measureDef.MeasureType!.Name,
				hierarchy = new()
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
				data.hierarchy.Add(hierarchyOwner);
				data.owner = measure.Owner;
			}

			returnObject.data.Add(data);
			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpPut]
	public ActionResult<JsonResult> Put([FromBody] MeasuresOwnerObject values) {
		MeasureIDReturnObject returnObject = new MeasureIDReturnObject {
			data = new List<MeasureTypeDataObject>()
		};

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.measure, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var measureDef = _context.MeasureDefinition
				.Include(d => d.MeasureType)
				.Where(md => md.Id == values.measureDefinitionId)
				.First();
			var data = new MeasureTypeDataObject {
				measureName = measureDef.Name,
				measureTypeName = _context.MeasureType.Find(measureDef.MeasureTypeId)?.Name
			};

			var hierarchies = from h in _context.Hierarchy
							  where h.HierarchyParentId == values.hierarchyId || h.Id == values.hierarchyId
							  orderby h.Id
							  select h;

			bool any = false;
			data.hierarchy = new List<RegionOwnerObject>();
			data.owner = values.owner;
			var lastUpdatedOn = DateTime.Now;
			foreach (var hierarchy in hierarchies) {
				data.hierarchy.Add(new() {
					id = hierarchy.Id,
					name = hierarchy.Name
				});

				var measure = _context.Measure
							  .Where(m => m.HierarchyId == hierarchy.Id && m.MeasureDefinition!.Id == values.measureDefinitionId).FirstOrDefault();

				if (measure != null) {
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
			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}
}
