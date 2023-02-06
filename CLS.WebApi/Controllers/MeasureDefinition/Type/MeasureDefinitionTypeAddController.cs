using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.MeasureDefinition.Type;

[Route("api/measureDefinition/type/[controller]")]
[Authorize]
[ApiController]
public class AddController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject? _user = new();

	public AddController(ApplicationDbContext context) {
		_context = context;
	}

	[HttpGet]
	public IEnumerable<string> Get() {
		return new string[] { "value1", "value2" };
	}

	[HttpGet("{id}")]
	public string Get(int id) {
		return "value";
	}

	[HttpPost]
	public ActionResult<JsonResult> Post([FromBody] MeasureTypeObject value) {
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.measureDefinition, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var returnObject = new MeasureTypeModel { data = new() };

			// Validates name
			int validateCount = _context.MeasureType.Where(m => m.Name.Trim().ToLower() == value.name.Trim().ToLower()).Count();
			if (validateCount > 0) {
				throw new Exception(Resource.VAL_MEASURE_TYPE_EXIST);
			}

			var lastUpdatedOn = DateTime.Now;
			var measureType = _context.MeasureType.Add(new() {
				Description = value.description,
				Name = value.name,
				LastUpdatedOn = lastUpdatedOn
			}).Entity;
			_context.SaveChanges();
			//value.id = _measureTypeRepository.All().Where(m => m.Name == value.name).First().Id;
			value.id = measureType.Id;

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-08",
				Resource.MEASURE_TYPE,
				@"Added / ID=" + measureType.Id.ToString(),
				lastUpdatedOn,
				_user.userId
			);

			returnObject.data = value;
			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpPut("{id}")]
	public void Put(int id, [FromBody] string value) {
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
