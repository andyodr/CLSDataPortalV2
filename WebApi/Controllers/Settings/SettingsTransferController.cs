using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Settings;

[ApiController]
[Route("api/settings/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public sealed class TransferController : ControllerBase
{
	private readonly ConfigSettings _config;
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public TransferController(IOptions<ConfigSettings> config, ApplicationDbContext context) {
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
				_ = _context.Database.ExecuteSql($"msdb.dbo.sp_start_job @job_name={_config.sQLJobSSIS}");
				_context.AddAuditTrail(Resource.WEB_PAGES, "WEB-09",
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
