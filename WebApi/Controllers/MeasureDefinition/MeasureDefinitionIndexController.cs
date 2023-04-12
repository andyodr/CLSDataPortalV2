using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers.MeasureDefinition;

[ApiController]
[Route("api/measureDefinition/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public class IndexController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public IndexController(ApplicationDbContext context) => _context = context;

	[HttpGet("{measureTypeId}")]
	public ActionResult<MeasureDefinitionIndexReturnObject> Get(int measureTypeId) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var returnObject = new MeasureDefinitionIndexReturnObject { Data = new() };

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
						   md.Calculated,
						   daily = md.AggDaily,
						   weekly = md.AggWeekly,
						   monthly = md.AggMonthly,
						   quarterly = md.AggQuarterly,
						   yearly = md.AggYearly,
						   md.ReportInterval,
						   aggFunction = md.AggFunction,
						   fieldNumber = md.FieldNumber
					   };

			foreach (var md in mDef.AsNoTrackingWithIdentityResolution()) {
				MeasureDefinitionEdit currentMD = new() {
					Id = md.id,
					Name = md.name,
					MeasureTypeId = measureTypeId,
					VarName = md.varName,
					Description = md.description,
					Expression = md.expression,
					Precision = md.precision,
					Priority = md.priority,
					FieldNumber = md.fieldNumber,
					UnitId = md.Unit.Id,
					Units = md.Unit.Short,
					Calculated = md.Calculated
				};

				currentMD.Interval = md.ReportInterval.Name;
				currentMD.IntervalId = md.ReportInterval.Id;
				currentMD.Daily = md.daily;
				currentMD.Weekly = md.weekly;
				currentMD.Monthly = md.monthly;
				currentMD.Quarterly = md.quarterly;
				currentMD.Yearly = md.yearly;
				switch ((Intervals)md.ReportInterval.Id) {
					case Intervals.Daily:
						currentMD.Daily = false;
						break;
					case Intervals.Weekly:
						currentMD.Weekly = false;
						break;
					case Intervals.Monthly:
						currentMD.Monthly = false;
						break;
					case Intervals.Quarterly:
						currentMD.Quarterly = false;
						break;
					case Intervals.Yearly:
						currentMD.Yearly = false;
						break;
				}

				var afn = AggregationFunctions.list.Find(x => x.Id == md.aggFunction!);
				currentMD.AggFunction = afn?.Name ?? string.Empty;
				currentMD.AggFunctionId = afn?.Id;
				returnObject.Data.Add(currentMD);
			}

			_user.savedFilters[pages.measureDefinition].measureTypeId = measureTypeId;
			//returnObject.filter = _user.savedFilters[pages.measureDefinition];

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_context, e, _user.Id));
		}
	}
}
