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
	private UserObject _user = new();

	public EditController(ApplicationDbContext context) {
		_context = context;
	}

	// GET api/values/5
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

			MeasureTypeModel returnObject = new() { data = new() };
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

	// POST api/values
	[HttpPost]
	public void Post([FromBody] string value) {
	}

	// PUT api/values/5
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

			MeasureTypeModel returnObject = new() { data = new MeasureTypeObject() };

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

				Helper.addAuditTrail(
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

	// DELETE api/values/5
	[HttpDelete("{id}")]
	public void Delete(int id) {
	}

}
