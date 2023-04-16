using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.MeasureDefinition.Type;

[ApiController]
[Route("/api/measuredefinition/type/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public class EditController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public EditController(ApplicationDbContext context) => _dbc = context;

	[HttpGet("{id:min(1)}")]
	public ActionResult<MeasureType> Get(int id) {
		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var measType = _dbc.MeasureType.Where(m => m.Id == id).Single();
			return new MeasureType(measType.Id, measType.Name, measType.Description);
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}

	[HttpPut]
	public ActionResult<MeasureTypeResult> Put(MeasureType body) {
		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			// Validates name
			int validateCount = _dbc.MeasureType
			  .Where(m => m.Id != body.Id && m.Name.Trim().ToLower() == body.Name.Trim().ToLower())
			  .Count();
			if (validateCount > 0) {
				BadRequest(Resource.VAL_MEASURE_TYPE_EXIST);
			}

			var measureType = _dbc.MeasureType.Where(m => m.Id == body.Id).FirstOrDefault();
			if (measureType is not null) {
				var lastUpdatedOn = DateTime.Now;

				measureType.Description = body.Description;
				measureType.Name = body.Name;
				measureType.LastUpdatedOn = lastUpdatedOn;
				_dbc.SaveChanges();

				AddAuditTrail(_dbc,
					Resource.WEB_PAGES,
					"WEB-08",
					Resource.MEASURE_TYPE,
					@"Updated / ID=" + measureType.Id.ToString(),
					lastUpdatedOn,
					_user.Id
				);
			}

			return new MeasureTypeResult(body.Id ?? -1,
				_dbc.MeasureType.Select(m => new MeasureType(m.Id, m.Name, m.Description)).ToArray()
			);
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
