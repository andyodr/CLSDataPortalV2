using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers.Settings;

[ApiController]
[Route("api/settings/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public class TransferController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public TransferController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_context = context;
	}

	[HttpPut]
	public ActionResult Put() {
		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			// Runs SQL Job
			try {
				_ = _context.Database.ExecuteSql($"EXEC msdb.dbo.sp_start_job @job_name={_config.sQLJobSSIS}");
				AddAuditTrail(_context,
					Resource.WEB_PAGES,
					"WEB-09",
					Resource.SETTINGS,
					@"Transfer / SQL Job Run=" + _config.sQLJobSSIS,
					DateTime.Now,
					_user.Id
				);

				return Ok();
			}
			catch {
				return BadRequest(string.Format($"Transfer / {Resource.SQL_JOB_ERR}", _config.sQLJobSSIS));
			}
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_context, e, _user.Id));
		}
	}
}
