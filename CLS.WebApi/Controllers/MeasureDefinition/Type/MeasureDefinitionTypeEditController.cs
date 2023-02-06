using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.MeasureDefinition.Type;

[Route("/api/measuredefinition/type/[controller]")]
[Authorize]
[ApiController]
public class EditController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject? _user = new();

	public EditController(ApplicationDbContext context) {
		_context = context;
	}

	[HttpGet("{id}")]
	public ActionResult<JsonResult> Get(int id) {

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.measureDefinition, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var returnObject = new MeasureTypeModel { data = new() };
			foreach (var measType in _context.MeasureType.Where(m => m.Id == id)) {
				returnObject.data.id = measType.Id;
				returnObject.data.name = measType.Name;
				returnObject.data.description = measType.Description;
			}

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpPost]
	public void Post([FromBody] string value) {
	}

	[HttpPut]
	public ActionResult<JsonResult> Put([FromBody] MeasureTypeObject value) {
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
			int validateCount = _context.MeasureType
			  .Where(m => m.Id != value.id && m.Name.Trim().ToLower() == value.name.Trim().ToLower())
			  .Count();
			if (validateCount > 0) {
				throw new Exception(Resource.VAL_MEASURE_TYPE_EXIST);
			}

			var measureType = _context.MeasureType.Where(m => m.Id == value.id).FirstOrDefault();
			if (measureType != null) {
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
			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
