using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers.MeasureDefinition.Type;

public record MeasureType(int? Id, string Name, string? Description);
public record MeasureTypeResult(int id, IList<MeasureType> measureTypes);

[ApiController]
[Route("api/measureDefinition/type/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public class AddController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public AddController(ApplicationDbContext context) => _dbc = context;

	[HttpPost]
	public ActionResult<MeasureTypeResult> Post(MeasureType body) {
		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			// Validates name
			int validateCount = _dbc.MeasureType
				.Where(m => m.Name.Trim().ToLower() == body.Name.Trim().ToLower()).Count();
			if (validateCount > 0) {
				BadRequest(Resource.VAL_MEASURE_TYPE_EXIST);
			}

			var lastUpdatedOn = DateTime.Now;
			var mtype = _dbc.MeasureType.Add(new() {
				Name = body.Name,
				Description = body.Description,
				LastUpdatedOn = lastUpdatedOn
			}).Entity;
			_dbc.SaveChanges();

			AddAuditTrail(_dbc,
				Resource.WEB_PAGES,
				"WEB-08",
				Resource.MEASURE_TYPE,
				@"Added / ID=" + mtype.Id.ToString(),
				lastUpdatedOn,
				_user.Id
			);

			return new MeasureTypeResult(mtype.Id,
				_dbc.MeasureType.Select(m => new MeasureType(m.Id, m.Name, m.Description)).ToArray()
			);
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
