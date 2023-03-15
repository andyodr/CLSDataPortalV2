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

			var returnObject = new RegionMetricsFilterObject { Data = new(), Hierarchy = new(), Levels = new() };
			var levels = from level in _context.HierarchyLevel.OrderBy(l => l.Id)
						 select new { id = level.Id, name = level.Name };

			foreach (var level in levels) {
				returnObject.Levels.Add(new() { Id = level.id, Name = level.name });
			}

			var regions = _context.Hierarchy.OrderBy(r => r.Id).AsNoTrackingWithIdentityResolution().ToArray();
			returnObject.Hierarchy.Add(new() {
				Hierarchy = regions.First().Name,
				Id = regions.First().Id,
				Sub = Helper.GetSubsAll(_context, regions.First().Id),
				Count = 0
			});

			//set regionid here for current user
			foreach (var hierarchy in regions) {
				var exists = (from measure in _context.Measure
							  from md in measure.MeasureData
							  where measure.HierarchyId == hierarchy.Id
							  select md.Id).Any();
				var newData = new RegionsDataViewModel {
					Id = hierarchy.Id,
					Name = hierarchy.Name,
					LevelId = hierarchy.HierarchyLevelId,
					Level = levels.Where(l => l.id == hierarchy.HierarchyLevelId).First().name,
					Active = hierarchy.Active ?? false,
					Remove = !exists
				};
				var parent = _context.Hierarchy.Where(h => h.Id == hierarchy.HierarchyParentId);
				if (parent.Any()) {
					var firstParent = parent.First();
					newData.ParentId = firstParent.Id;
					newData.ParentName = firstParent.Name;
				}
				else {
					newData.ParentId = null;
					newData.ParentName = "";
				}

				returnObject.Data.Add(newData);
			}

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}

	}

	[HttpPost]
	public ActionResult<RegionMetricsFilterObject> Post(RegionsDataViewModelAdd dto) {
		var returnObject = new RegionMetricsFilterObject { Data = new(), Hierarchy = new() };

		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var updatedHierarchy = _context.Hierarchy.Add(new() {
				Name = dto.Name,
				HierarchyParentId = dto.ParentId,
				HierarchyLevelId = dto.LevelId,
				Active = dto.Active,
				IsProcessed = 2,
				LastUpdatedOn = DateTime.Now
			}).Entity;
			_ = _context.SaveChanges();
			var newHierarchy = new RegionsDataViewModel {
				Id = updatedHierarchy.Id,
				Name = updatedHierarchy.Name,
				LevelId = updatedHierarchy.HierarchyLevelId,
				Level = _context.HierarchyLevel.Where(h => h.Id == updatedHierarchy.HierarchyLevelId).First().Name,
				ParentId = updatedHierarchy.HierarchyParentId,
				Active = updatedHierarchy.Active ?? false
			};

			var parent = _context.Hierarchy.Where(h => h.Id == updatedHierarchy.HierarchyParentId).FirstOrDefault();
			newHierarchy.ParentId = parent?.Id;
			newHierarchy.ParentName = parent?.Name ?? string.Empty;

			string measuresAndTargets = Helper.CreateMeasuresAndTargets(_context, _user.userId, newHierarchy.Id);
			_context.SaveChanges();
			if (!string.IsNullOrEmpty(measuresAndTargets)) {
				throw new Exception(measuresAndTargets);
			}

			var exists = (from measure in _context.Measure
						  from md in measure.MeasureData
						  where measure.HierarchyId == newHierarchy.Id
						  select md.Id).Any();
			newHierarchy.Remove = !exists;
			returnObject.Data.Add(newHierarchy);

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-05",
				Resource.HIERARCHY,
				@"Added / ID=" + newHierarchy.Id.ToString(),
				DateTime.Now,
				_user.userId
			);

			var regions = _context.Hierarchy.OrderBy(r => r.Id).ToArray();
			returnObject.Hierarchy.Add(new() {
				Hierarchy = regions.First().Name,
				Id = regions.First().Id,
				Sub = Helper.GetSubsAll(_context, regions.First().Id),
				Count = 0
			});

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpPut]
	public ActionResult<RegionMetricsFilterObject> Put(RegionsDataViewModel dto) {
		var returnObject = new RegionMetricsFilterObject { Data = new() };
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			DateTime updatedOn = DateTime.Now;
			var updateHierarchy = _context.Hierarchy.Find(dto.Id);
			if (updateHierarchy is null) {
				return returnObject;
			}

			updateHierarchy.Name = dto.Name;
			updateHierarchy.Active = dto.Active;
			updateHierarchy.HierarchyParentId = dto.ParentId;
			updateHierarchy.LastUpdatedOn = updatedOn;
			updateHierarchy.HierarchyLevelId = dto.LevelId;
			if (updateHierarchy.HierarchyLevelId == Helper.hierarchyGlobalId) {
				updateHierarchy.HierarchyParentId = null;
			}

			updateHierarchy.IsProcessed = (byte)Helper.IsProcessed.complete;
			_context.SaveChanges();

			var newHierarchy = new RegionsDataViewModel {
				Id = updateHierarchy.Id,
				Name = updateHierarchy.Name,
				LevelId = updateHierarchy.HierarchyLevelId,
				Active = updateHierarchy.Active ?? false,
				Level = _context.HierarchyLevel.Where(h => h.Id == updateHierarchy.HierarchyLevelId).First().Name
			};
			var exists = (from measure in _context.Measure
						  from md in measure.MeasureData
						  where measure.HierarchyId == newHierarchy.Id
						  select md.Id).Any();
			newHierarchy.Remove = !exists;
			var parent = _context.Hierarchy.Where(h => h.Id == dto.ParentId).FirstOrDefault();
			newHierarchy.ParentId = parent?.Id;
			newHierarchy.ParentName = parent?.Name ?? string.Empty;

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-05",
				Resource.HIERARCHY,
				@"Updated / ID=" + newHierarchy.Id.ToString(),
				updatedOn,
				_user.userId
			);

			returnObject.Data.Add(newHierarchy);
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

			var returnObject = new RegionMetricsFilterObject { Data = new(), Hierarchy = new() };
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

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-05",
				Resource.HIERARCHY,
				@"Deleted / ID=" + id.ToString() +
						" / Name=" + hierarchyName,
				DateTime.Now,
				_user.userId
			);

			var regions = _context.Hierarchy.OrderBy(r => r.Id).ToArray();
			returnObject.Hierarchy.Add(new() {
				Hierarchy = regions.First().Name,
				Id = regions.First().Id,
				Sub = Helper.GetSubsAll(_context, regions.First().Id), Count = 0
			});
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId)); ;
		}
	}
}
