using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CLS.WebApi.Controllers.Settings;

[Route("api/settings/[controller]")]
[Authorize]
[ApiController]
public class TransferController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _context;
	private UserObject _user = new();

	public TransferController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_context = context;
	}

	// PUT api/values/5
	[HttpPut]
	public ActionResult<JsonResult> Put([FromBody] dynamic jsonString) {
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null)
				throw new Exception();
			if (!Helper.IsUserPageAuthorized(Helper.pages.settings, _user.userRoleId))
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);

			// Runs SQL Job
			if (Helper.StartSQLJob(_config.sQLJobSSIS)) {
				Helper.addAuditTrail(
				  Resource.WEB_PAGES,
				   "WEB-09",
				   Resource.SETTINGS,
				   @"Transfer / SQL Job Run=" + _config.sQLJobSSIS,
				   DateTime.Now,
				   _user.userId
				);

				return new JsonResult(null);
			}
			else {
				throw new Exception(string.Format($"Transfer / {Resource.SQL_JOB_ERR}", _config.sQLJobSSIS));
			}
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}
}
