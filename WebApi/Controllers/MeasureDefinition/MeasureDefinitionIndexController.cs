using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.MeasureDefinition;

[ApiController]
[Route("api/measureDefinition/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public sealed class IndexController : BaseController
{
	[HttpGet("{measureTypeId}")]
	public ActionResult<MeasureDefinitionIndexResponse> Get(int measureTypeId) {
		if (CreateUserObject(User) is not UserDto _user) {
			return Unauthorized();
		}

		try {
			MeasureDefinitionIndexResponse returnObject = new() { Data = [] };

			var mDef = from md in Dbc.MeasureDefinition
					   where md.MeasureTypeId == measureTypeId
					   orderby md.FieldNumber, md.Priority, md.Name
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
					Calculated = md.Calculated,
					Interval = md.ReportInterval.Name,
					IntervalId = md.ReportInterval.Id,
					Daily = md.daily,
					Weekly = md.weekly,
					Monthly = md.monthly,
					Quarterly = md.quarterly,
					Yearly = md.yearly
				};
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

				var afn = AggregationFunctions.List.Find(x => x.Id == md.aggFunction!);
				currentMD.AggFunction = afn?.Name ?? string.Empty;
				currentMD.AggFunctionId = afn?.Id;
				returnObject.Data.Add(currentMD);
			}

			_user.savedFilters[Pages.MeasureDefinition].measureTypeId = measureTypeId;

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}
}
