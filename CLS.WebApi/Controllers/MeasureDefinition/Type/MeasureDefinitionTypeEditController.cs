using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers.MeasureDefinition.Type;

[ApiController]
[Route("/api/measuredefinition/type/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public class EditController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public EditController(ApplicationDbContext context) => _context = context;

	[HttpGet("{id}")]
	public ActionResult<MeasureTypeModel> Get(int id) {
		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var returnObject = new MeasureTypeModel { Data = new() };
			foreach (var measType in _context.MeasureType.Where(m => m.Id == id)) {
				returnObject.Data.Id = measType.Id;
				returnObject.Data.Name = measType.Name;
				returnObject.Data.Description = measType.Description;
			}

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_context, e, _user.Id));
		}
	}

	[HttpPut]
	public ActionResult<MeasureTypeModel> Put(MeasureTypeObject value) {
		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var returnObject = new MeasureTypeModel { Data = new() };

			// Validates name
			int validateCount = _context.MeasureType
			  .Where(m => m.Id != value.Id && m.Name.Trim().ToLower() == value.Name.Trim().ToLower())
			  .Count();
			if (validateCount > 0) {
				BadRequest(Resource.VAL_MEASURE_TYPE_EXIST);
			}

			var measureType = _context.MeasureType.Where(m => m.Id == value.Id).FirstOrDefault();
			if (measureType is not null) {
				var lastUpdatedOn = DateTime.Now;

				measureType.Description = value.Description;
				measureType.Name = value.Name;
				measureType.LastUpdatedOn = lastUpdatedOn;
				_context.SaveChanges();

				AddAuditTrail(_context,
					Resource.WEB_PAGES,
					"WEB-08",
					Resource.MEASURE_TYPE,
					@"Updated / ID=" + measureType.Id.ToString(),
					lastUpdatedOn,
					_user.Id
				);
			}

			returnObject.Data = value;
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_context, e, _user.Id));
		}
	}
}
