using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.MeasureDefinition.Type;

[ApiController]
[Route("/api/measuredefinition/type/[controller]")]
[Authorize(Roles = "Regional Administrator, System Administrator")]
public class EditController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public EditController(ApplicationDbContext context) => _context = context;

	[HttpGet("{id}")]
	public ActionResult<MeasureTypeModel> Get(int id) {
		try {
			if (Helper.UserAuthorization(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var returnObject = new MeasureTypeModel { data = new() };
			foreach (var measType in _context.MeasureType.Where(m => m.Id == id)) {
				returnObject.data.id = measType.Id;
				returnObject.data.name = measType.Name;
				returnObject.data.description = measType.Description;
			}

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpPost]
	public void Post([FromBody] string value) {
	}

	[HttpPut]
	public ActionResult<MeasureTypeModel> Put([FromBody] MeasureTypeObject value) {
		try {
			if (Helper.UserAuthorization(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var returnObject = new MeasureTypeModel { data = new() };

			// Validates name
			int validateCount = _context.MeasureType
			  .Where(m => m.Id != value.id && m.Name.Trim().ToLower() == value.name.Trim().ToLower())
			  .Count();
			if (validateCount > 0) {
				BadRequest(Resource.VAL_MEASURE_TYPE_EXIST);
			}

			var measureType = _context.MeasureType.Where(m => m.Id == value.id).FirstOrDefault();
			if (measureType is not null) {
				var lastUpdatedOn = DateTime.Now;

				measureType.Description = value.description;
				measureType.Name = value.name;
				measureType.LastUpdatedOn = lastUpdatedOn;
				_context.SaveChanges();

				Helper.AddAuditTrail(_context,
					Resource.WEB_PAGES,
					"WEB-08",
					Resource.MEASURE_TYPE,
					@"Updated / ID=" + measureType.Id.ToString(),
					lastUpdatedOn,
					_user.userId
				);
			}

			returnObject.data = value;
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
