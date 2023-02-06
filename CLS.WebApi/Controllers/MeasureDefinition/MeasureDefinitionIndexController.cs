using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.MeasureDefinition;

[Route("api/measureDefinition/[controller]")]
[Authorize]
[ApiController]
public class IndexController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject? _user = new();

	public IndexController(ApplicationDbContext context) {
		_context = context;
	}

	[HttpGet]
	public ActionResult<JsonResult> Get(int measureTypeId) {
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.measureDefinition, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var returnObject = new MeasureDefinitionIndexReturnObject { data = new() };

			//_userRepository.Find(u => u.Id == record.userId).UserName

			var mDef = from md in _context.MeasureDefinition
					   where md.MeasureTypeId == measureTypeId
					   orderby md.FieldNumber ascending, md.Name
					   select new
					   {
						   id = md.Id,
						   name = md.Name,
						   varName = md.VariableName,
						   description = md.Description,
						   expression = md.Expression,
						   precision = md.Precision,
						   priority = md.Priority,
						   md.Unit,
						   calculated = md.Calculated,
						   daily = md.AggDaily,
						   weekly = md.AggWeekly,
						   monthly = md.AggMonthly,
						   quarterly = md.AggQuarterly,
						   yearly = md.AggYearly,
						   interval = md.ReportInterval,
						   aggFunction = md.AggFunction,
						   fieldNumber = md.FieldNumber
					   };

			foreach (var md in mDef.AsNoTracking()) {
				var currentMD = new MeasureDefinitionViewModel {
					id = md.id,
					name = md.name,
					varName = md.varName,
					description = md.description,
					expression = md.expression,
					precision = md.precision,
					priority = md.priority,
					fieldNumber = md.fieldNumber,
					unitId = md.Unit.Id,
					units = md.Unit.Short
				};

				if (md.calculated == null) {
					currentMD.calculated = null;
				}
				else {
					currentMD.calculated = (bool)md.calculated;
				}

				currentMD.interval = md.interval.Name;
				currentMD.daily = md.daily;
				currentMD.weekly = md.weekly;
				currentMD.monthly = md.monthly;
				currentMD.quarterly = md.quarterly;
				currentMD.yearly = md.yearly;
				switch ((Helper.intervals)md.interval.Id) {
					case Helper.intervals.daily:
						currentMD.daily = false;
						break;
					case Helper.intervals.weekly:
						currentMD.weekly = false;
						break;
					case Helper.intervals.monthly:
						currentMD.monthly = false;
						break;
					case Helper.intervals.quarterly:
						currentMD.quarterly = false;
						break;
					case Helper.intervals.yearly:
						currentMD.yearly = false;
						break;
				}

				var afn = AggregationFunctions.list.Find(x => x.Id == md.aggFunction!);
				currentMD.aggFunction = afn?.Name ?? string.Empty;
				returnObject.data.Add(currentMD);
			}

			_user.savedFilters[Helper.pages.measureDefinition].measureTypeId = measureTypeId;
			//returnObject.filter = _user.savedFilters[Helper.pages.measureDefinition];

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}

	}

	[HttpPost]
	public void Post([FromBody] string value) {
	}

	[HttpPut("{id}")]
	public void Put(int id, [FromBody] string value) {
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
