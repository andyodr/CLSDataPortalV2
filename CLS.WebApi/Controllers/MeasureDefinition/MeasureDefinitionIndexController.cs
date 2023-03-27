using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.MeasureDefinition;

[ApiController]
[Route("api/measureDefinition/[controller]")]
[Authorize(Roles = "Regional Administrator, System Administrator")]
public class IndexController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public IndexController(ApplicationDbContext context) => _context = context;

	[HttpGet("{measureTypeId}")]
	public ActionResult<MeasureDefinitionIndexReturnObject> Get(int measureTypeId) {
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var returnObject = new MeasureDefinitionIndexReturnObject { Data = new() };

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
				var currentMD = new MeasureDefinitionViewModel {
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
				switch ((Helper.intervals)md.ReportInterval.Id) {
					case Helper.intervals.daily:
						currentMD.Daily = false;
						break;
					case Helper.intervals.weekly:
						currentMD.Weekly = false;
						break;
					case Helper.intervals.monthly:
						currentMD.Monthly = false;
						break;
					case Helper.intervals.quarterly:
						currentMD.Quarterly = false;
						break;
					case Helper.intervals.yearly:
						currentMD.Yearly = false;
						break;
				}

				var afn = AggregationFunctions.list.Find(x => x.Id == md.aggFunction!);
				currentMD.AggFunction = afn?.Name ?? string.Empty;
				currentMD.AggFunctionId = afn?.Id;
				returnObject.Data.Add(currentMD);
			}

			_user.savedFilters[Helper.pages.measureDefinition].measureTypeId = measureTypeId;
			//returnObject.filter = _user.savedFilters[Helper.pages.measureDefinition];

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.Id));
		}
	}
}
