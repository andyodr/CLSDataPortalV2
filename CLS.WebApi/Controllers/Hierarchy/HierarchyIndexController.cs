using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.Hierarchy;

[ApiController]
[Route("api/hierarchy/[controller]")]
[Authorize(Roles = "System Administrator")]
public class IndexController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public IndexController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public ActionResult<RegionMetricsFilterObject> Get() {
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var returnObject = new RegionMetricsFilterObject { data = new(), hierarchy = new(), levels = new() };
			var levels = from level in _context.HierarchyLevel.OrderBy(l => l.Id)
						 select new { id = level.Id, name = level.Name };

			foreach (var level in levels) {
				returnObject.levels.Add(new() { id = level.id, name = level.name });
			}

			var regions = _context.Hierarchy.OrderBy(r => r.Id).AsNoTrackingWithIdentityResolution().ToArray();
			returnObject.hierarchy.Add(new() {
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

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}

	}

	[HttpPost]
	public ActionResult<RegionMetricsFilterObject> Post(RegionsDataViewModelAdd value) {
		var returnObject = new RegionMetricsFilterObject { data = new(), hierarchy = new() };

		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
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
			newHierarchy.parentId = parent?.Id;
			newHierarchy.parentName = parent?.Name ?? string.Empty;

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

			var regions = _context.Hierarchy.OrderBy(r => r.Id).ToArray();
			returnObject.hierarchy.Add(new() {
				hierarchy = regions.First().Name,
				id = regions.First().Id,
				sub = Helper.GetSubsAll(_context, regions.First().Id),
				count = 0
			});

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpPut]
	public ActionResult<RegionMetricsFilterObject> Put(RegionsDataViewModel value) {
		var returnObject = new RegionMetricsFilterObject { data = new() };
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			DateTime updatedOn = DateTime.Now;
			var updateHierarchy = _context.Hierarchy.Find(value.id);
			if (updateHierarchy is null) {
				return returnObject;
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
			newHierarchy.parentId = parent?.Id;
			newHierarchy.parentName = parent?.Name ?? string.Empty;

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-05",
				Resource.HIERARCHY,
				@"Updated / ID=" + newHierarchy.id.ToString(),
				updatedOn,
				_user.userId
			);

			returnObject.data.Add(newHierarchy);
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpDelete("{id}")]
	public ActionResult<RegionMetricsFilterObject> Delete(int id) {
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
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
				var measures = _context.Measure.Where(m => m.HierarchyId == id).ToArray();
				var targets = _context.Target.Where(t => measures.Any(m => m.Id == t.MeasureId)).ToArray();

				// delete Targets
				if (targets.Length > 0) {
					foreach (var item in targets) {
						_context.Target.Remove(item);
					}

					_context.SaveChanges();
				}

				// delete Measures
				if (measures.Length > 0) {
					foreach (var item in measures) {
						_context.Measure.Remove(item);
					}

					_context.SaveChanges();
				}

				// Find Hierarchy Name before deletion
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

			var regions = _context.Hierarchy.OrderBy(r => r.Id).ToArray();
			returnObject.hierarchy.Add(new() {
				hierarchy = regions.First().Name,
				id = regions.First().Id,
				sub = Helper.GetSubsAll(_context, regions.First().Id), count = 0
			});
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId)); ;
		}
	}
}
