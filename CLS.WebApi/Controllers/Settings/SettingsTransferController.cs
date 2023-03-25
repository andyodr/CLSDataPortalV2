using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CLS.WebApi.Controllers.Settings;

[ApiController]
[Route("api/settings/[controller]")]
[Authorize(Roles = "System Administrator")]
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
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			// Runs SQL Job
			try {
				_ = _context.Database.ExecuteSql($"EXEC msdb.dbo.sp_start_job @job_name={_config.sQLJobSSIS}");
				Helper.AddAuditTrail(_context,
					Resource.WEB_PAGES,
					"WEB-09",
					Resource.SETTINGS,
					@"Transfer / SQL Job Run=" + _config.sQLJobSSIS,
					DateTime.Now,
					_user.userId
				);

				return Ok();
			}
			catch {
				return BadRequest(string.Format($"Transfer / {Resource.SQL_JOB_ERR}", _config.sQLJobSSIS));
			}
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}
}
