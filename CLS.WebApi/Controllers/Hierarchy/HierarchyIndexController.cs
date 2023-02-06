using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.Hierarchy;

[Route("api/hierarchy/[controller]")]
[Authorize]
[ApiController]
public class IndexController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject? _user = new();

	public IndexController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public ActionResult<JsonResult> Get() {
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.hierarchy, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var returnObject = new RegionMetricsFilterObject { data = new(), hierarchy = new(), levels = new() };
			var levels = from level in _context.HierarchyLevel.OrderBy(l => l.Id)
						 select new { id = level.Id, name = level.Name };

			foreach (var level in levels) {
				returnObject.levels.Add(new() { id = level.id, name = level.name });
			}

			var regions = _context.Hierarchy.OrderBy(r => r.Id).AsNoTrackingWithIdentityResolution().ToList();
			returnObject.hierarchy.Add(new RegionFilterObject {
				hierarchy = regions.First().Name,
				id = regions.First().Id,
				sub = Helper.GetSubsAll(_context, regions.First().Id),
				count = 0
			});

			//set regionid here for current user
			foreach (var hierarchy in regions) {
				var exists = (from measure in _context.Measure
							  from md in measure.MeasureData
							  where measure.HierarchyId == hierarchy.Id
							  select md.Id).Any();
				var newData = new RegionsDataViewModel {
					id = hierarchy.Id,
					name = hierarchy.Name,
					levelId = hierarchy.HierarchyLevelId,
					level = levels.Where(l => l.id == hierarchy.HierarchyLevelId).First().name,
					active = hierarchy.Active,
					remove = !exists
				};
				var parent = _context.Hierarchy.Where(h => h.Id == hierarchy.HierarchyParentId);
				if (parent.Any()) {
					var firstParent = parent.First();
					newData.parentId = firstParent.Id;
					newData.parentName = firstParent.Name;
				}
				else {
					newData.parentId = null;
					newData.parentName = "";
				}

				returnObject.data.Add(newData);
			}

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}

	}

	[HttpGet("{id}")]
	public string Get(int id) => "value";

	[HttpPost]
	public ActionResult<JsonResult> Post([FromBody] RegionsDataViewModelAdd value) {
		var returnObject = new RegionMetricsFilterObject { data = new(), hierarchy = new() };

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			var updatedHierarchy = _context.Hierarchy.Add(new() {
				Name = value.name,
				HierarchyParentId = value.parentId,
				HierarchyLevelId = value.levelId,
				Active = value.active,
				IsProcessed = 2,
				LastUpdatedOn = DateTime.Now
			}).Entity;
			_ = _context.SaveChanges();
			var newHierarchy = new RegionsDataViewModel {
				id = updatedHierarchy.Id,
				name = updatedHierarchy.Name,
				levelId = updatedHierarchy.HierarchyLevelId,
				level = _context.HierarchyLevel.Where(h => h.Id == updatedHierarchy.HierarchyLevelId).First().Name,
				parentId = updatedHierarchy.HierarchyParentId,
				active = updatedHierarchy.Active
			};

			var parent = _context.Hierarchy.Where(h => h.Id == updatedHierarchy.HierarchyParentId).FirstOrDefault();
			if (parent == null) {
				newHierarchy.parentId = null;
				newHierarchy.parentName = "";
			}
			else {
				newHierarchy.parentId = parent.Id;
				newHierarchy.parentName = parent.Name;
			}

			string measuresAndTargets = Helper.CreateMeasuresAndTargets(_context, _user.userId, newHierarchy.id);
			_context.SaveChanges();
			if (!string.IsNullOrEmpty(measuresAndTargets)) {
				throw new Exception(measuresAndTargets);
			}

			var exists = (from measure in _context.Measure
						  from md in measure.MeasureData
						  where measure.HierarchyId == newHierarchy.id
						  select md.Id).Any();
			newHierarchy.remove = !exists;
			returnObject.data.Add(newHierarchy);

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-05",
				Resource.HIERARCHY,
				@"Added / ID=" + newHierarchy.id.ToString(),
				DateTime.Now,
				_user.userId
			);

			var regions = _context.Hierarchy.OrderBy(r => r.Id).ToList();
			returnObject.hierarchy.Add(new RegionFilterObject { hierarchy = regions.ElementAt(0).Name, id = regions.ElementAt(0).Id, sub = Helper.GetSubsAll(_context, regions.First().Id), count = 0 });

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpPut]
	public ActionResult<JsonResult> Put([FromBody] RegionsDataViewModel value) {
		var returnObject = new RegionMetricsFilterObject { data = new() };
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			DateTime updatedOn = DateTime.Now;
			var updateHierarchy = _context.Hierarchy.Find(value.id);
			if (updateHierarchy == null) {
				return new JsonResult(returnObject);
			}

			updateHierarchy.Name = value.name;
			updateHierarchy.Active = value.active;
			updateHierarchy.HierarchyParentId = value.parentId;
			updateHierarchy.LastUpdatedOn = updatedOn;
			updateHierarchy.HierarchyLevelId = value.levelId ?? -1;
			if (updateHierarchy.HierarchyLevelId == Helper.hierarchyGlobalId) {
				updateHierarchy.HierarchyParentId = null;
			}

			updateHierarchy.IsProcessed = (byte)Helper.IsProcessed.complete;
			_context.SaveChanges();

			var newHierarchy = new RegionsDataViewModel {
				id = updateHierarchy.Id,
				name = updateHierarchy.Name,
				levelId = updateHierarchy.HierarchyLevelId,
				active = updateHierarchy.Active,
				level = _context.HierarchyLevel.Where(h => h.Id == updateHierarchy.HierarchyLevelId).First().Name
			};
			var exists = (from measure in _context.Measure
						  from md in measure.MeasureData
						  where measure.HierarchyId == newHierarchy.id
						  select md.Id).Any();
			newHierarchy.remove = !exists;
			var parent = _context.Hierarchy.Where(h => h.Id == value.parentId).FirstOrDefault();
			if (parent == null) {
				newHierarchy.parentId = null;
				newHierarchy.parentName = "";
			}
			else {
				newHierarchy.parentId = parent.Id;
				newHierarchy.parentName = parent.Name;
			}

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-05",
				Resource.HIERARCHY,
				@"Updated / ID=" + newHierarchy.id.ToString(),
				updatedOn,
				_user.userId
			);

			returnObject.data.Add(newHierarchy);
			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpDelete("{id}")]
	public ActionResult<JsonResult> Delete(int id) {
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			var returnObject = new RegionMetricsFilterObject { data = new(), hierarchy = new() };
			string hierarchyName = string.Empty;
			var exists = (from measure in _context.Measure
						  from md in measure.MeasureData
						  where measure.HierarchyId == id
						  select md.Id).Any();
			if (exists) {
				throw new Exception(Resource.HIERARCHY_ERR_DELETE);
			}
			else {
				// Find measures and Targets
				var measures = _context.Measure.Where(m => m.HierarchyId == id).ToList();
				var targets = _context.Target.Where(t => measures.Any(m => m.Id == t.MeasureId)).ToList();

				// delete Targets
				if (targets.Count > 0) {
					foreach (var item in targets) {
						_context.Target.Remove(item);
					}

					_context.SaveChanges();
				}

				// delete Measures
				if (measures.Count > 0) {
					foreach (var item in measures) {
						_context.Measure.Remove(item);
					}

					_context.SaveChanges();
				}

				// Find Hierarhy Name before deletion
				hierarchyName = _context.Hierarchy.Where(h => h.Id == id).First().Name;

				// delete Hierarchy
				_ = _context.Hierarchy.Remove(new() { Id = id });
				_context.SaveChanges();
			}

			var newHierarchy = new RegionsDataViewModel {
				id = id,
				name = "",
				levelId = null,
				parentId = null,
				remove = true,
				parentName = ""
			};
			returnObject.data.Add(newHierarchy);


			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-05",
				Resource.HIERARCHY,
				@"Deleted / ID=" + newHierarchy.id.ToString() +
						" / Name=" + hierarchyName,
				DateTime.Now,
				_user.userId
			);

			var regions = _context.Hierarchy.OrderBy(r => r.Id).ToList();
			returnObject.hierarchy.Add(new RegionFilterObject { hierarchy = regions.ElementAt(0).Name, id = regions.ElementAt(0).Id, sub = Helper.GetSubsAll(_context, regions.First().Id), count = 0 });

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user)); ;
		}
	}
}
