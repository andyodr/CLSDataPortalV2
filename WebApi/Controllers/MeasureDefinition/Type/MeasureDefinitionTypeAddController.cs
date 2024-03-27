using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.MeasureDefinition.Type;

public record MeasureType(int? Id, string Name, string? Description);
public record MeasureTypeResult(int Id, IList<MeasureType> MeasureTypes);

[ApiController]
[Route("api/measureDefinition/type/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public sealed class AddController : BaseController
{
	[HttpPost]
	public ActionResult<MeasureTypeResult> Post(MeasureType body) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			// Validates name
			int validateCount = Dbc.MeasureType
				.Where(m => m.Name.Trim().ToLower() == body.Name.Trim().ToLower()).Count();
			if (validateCount > 0) {
				BadRequest(Resource.VAL_MEASURE_TYPE_EXIST);
			}

			var lastUpdatedOn = DateTime.Now;
			var mtype = Dbc.MeasureType.Add(new() {
				Name = body.Name,
				Description = body.Description,
				LastUpdatedOn = lastUpdatedOn
			}).Entity;
			Dbc.SaveChanges();

			AddAuditTrail(Dbc,
				Resource.WEB_PAGES,
				"WEB-08",
				Resource.MEASURE_TYPE,
				@"Added / ID=" + mtype.Id.ToString(),
				lastUpdatedOn,
				_user.Id
			);

			return new MeasureTypeResult(mtype.Id,
                [.. Dbc.MeasureType.Select(m => new MeasureType(m.Id, m.Name, m.Description))]
            );
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}
}
