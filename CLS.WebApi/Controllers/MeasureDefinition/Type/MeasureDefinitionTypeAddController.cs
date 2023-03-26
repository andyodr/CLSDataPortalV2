using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.MeasureDefinition.Type;

[ApiController]
[Route("api/measureDefinition/type/[controller]")]
[Authorize(Roles = "Regional Administrator, System Administrator")]
public class AddController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public AddController(ApplicationDbContext context) => _context = context;

	[HttpPost]
	public ActionResult<MeasureTypeModel> Post(MeasureTypeObject value) {
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var returnObject = new MeasureTypeModel { data = new() };

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

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-08",
				Resource.MEASURE_TYPE,
				@"Added / ID=" + measureType.Id.ToString(),
				lastUpdatedOn,
				_user.Id
			);

			returnObject.data = value;
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.Id));
		}
	}
}
