using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers.MeasureDefinition.Type;

[ApiController]
[Route("api/measureDefinition/type/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public class AddController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public AddController(ApplicationDbContext context) => _context = context;

	[HttpPost]
	public ActionResult<MeasureTypeModel> Post(MeasureTypeObject value) {
		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var returnObject = new MeasureTypeModel { Data = new() };

			// Validates name
			int validateCount = _context.MeasureType.Where(m => m.Name.Trim().ToLower() == value.Name.Trim().ToLower()).Count();
			if (validateCount > 0) {
				BadRequest(Resource.VAL_MEASURE_TYPE_EXIST);
			}

			var lastUpdatedOn = DateTime.Now;
			var measureType = _context.MeasureType.Add(new() {
				Description = value.Description,
				Name = value.Name,
				LastUpdatedOn = lastUpdatedOn
			}).Entity;
			_context.SaveChanges();
			//value.id = _measureTypeRepository.All().Where(m => m.Name == value.name).First().Id;
			value.Id = measureType.Id;

			AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-08",
				Resource.MEASURE_TYPE,
				@"Added / ID=" + measureType.Id.ToString(),
				lastUpdatedOn,
				_user.Id
			);

			returnObject.Data = value;
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_context, e, _user.Id));
		}
	}
}
