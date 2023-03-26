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
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public IndexController(ApplicationDbContext context) => _dbc = context;

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
			var levels = from level in _dbc.HierarchyLevel.OrderBy(l => l.Id)
						 select new { id = level.Id, name = level.Name };

			foreach (var level in levels) {
				returnObject.Levels.Add(new() { Id = level.id, Name = level.name });
			}

			var regions = _dbc.Hierarchy.OrderBy(r => r.Id).AsNoTrackingWithIdentityResolution().ToArray();
			returnObject.Hierarchy.Add(new() {
				Hierarchy = regions.First().Name,
				Id = regions.First().Id,
				Sub = Helper.GetSubsAll(_dbc, regions.First().Id),
				Count = 0
			});

			//set regionid here for current user
			foreach (var hierarchy in regions) {
				var exists = (from measure in _dbc.Measure
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
				var parent = _dbc.Hierarchy.Where(h => h.Id == hierarchy.HierarchyParentId);
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
			return BadRequest(Helper.ErrorProcessing(_dbc, e, _user.Id));
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

			var updatedHierarchy = _dbc.Hierarchy.Add(new() {
				Name = dto.Name,
				HierarchyParentId = dto.ParentId,
				HierarchyLevelId = dto.LevelId,
				Active = dto.Active,
				IsProcessed = 2,
				LastUpdatedOn = DateTime.Now
			}).Entity;
			_ = _dbc.SaveChanges();
			var newHierarchy = new RegionsDataViewModel {
				Id = updatedHierarchy.Id,
				Name = updatedHierarchy.Name,
				LevelId = updatedHierarchy.HierarchyLevelId,
				Level = _dbc.HierarchyLevel.Where(h => h.Id == updatedHierarchy.HierarchyLevelId).First().Name,
				ParentId = updatedHierarchy.HierarchyParentId,
				Active = updatedHierarchy.Active ?? false
			};

			var parent = _dbc.Hierarchy.Where(h => h.Id == updatedHierarchy.HierarchyParentId).FirstOrDefault();
			newHierarchy.ParentId = parent?.Id;
			newHierarchy.ParentName = parent?.Name ?? string.Empty;

			string measuresAndTargets = Helper.CreateMeasuresAndTargets(_dbc, _user.Id, newHierarchy.Id);
			_dbc.SaveChanges();
			if (!string.IsNullOrEmpty(measuresAndTargets)) {
				throw new Exception(measuresAndTargets);
			}

			var exists = (from measure in _dbc.Measure
						  from md in measure.MeasureData
						  where measure.HierarchyId == newHierarchy.Id
						  select md.Id).Any();
			newHierarchy.Remove = !exists;
			returnObject.Data.Add(newHierarchy);

			Helper.AddAuditTrail(_dbc,
				Resource.WEB_PAGES,
				"WEB-05",
				Resource.HIERARCHY,
				@"Added / ID=" + newHierarchy.Id.ToString(),
				DateTime.Now,
				_user.Id
			);

			var regions = _dbc.Hierarchy.OrderBy(r => r.Id).ToArray();
			returnObject.Hierarchy.Add(new() {
				Hierarchy = regions.First().Name,
				Id = regions.First().Id,
				Sub = Helper.GetSubsAll(_dbc, regions.First().Id),
				Count = 0
			});

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_dbc, e, _user.Id));
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
			var updateHierarchy = _dbc.Hierarchy.Find(dto.Id);
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
			_dbc.SaveChanges();

			var newHierarchy = new RegionsDataViewModel {
				Id = updateHierarchy.Id,
				Name = updateHierarchy.Name,
				LevelId = updateHierarchy.HierarchyLevelId,
				Active = updateHierarchy.Active ?? false,
				Level = _dbc.HierarchyLevel.Where(h => h.Id == updateHierarchy.HierarchyLevelId).First().Name
			};
			var exists = (from measure in _dbc.Measure
						  from md in measure.MeasureData
						  where measure.HierarchyId == newHierarchy.Id
						  select md.Id).Any();
			newHierarchy.Remove = !exists;
			var parent = _dbc.Hierarchy.Where(h => h.Id == dto.ParentId).FirstOrDefault();
			newHierarchy.ParentId = parent?.Id;
			newHierarchy.ParentName = parent?.Name ?? string.Empty;

			Helper.AddAuditTrail(_dbc,
				Resource.WEB_PAGES,
				"WEB-05",
				Resource.HIERARCHY,
				@"Updated / ID=" + newHierarchy.Id.ToString(),
				updatedOn,
				_user.Id
			);

			returnObject.Data.Add(newHierarchy);
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_dbc, e, _user.Id));
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
			var exists = (from measure in _dbc.Measure
						  from md in measure.MeasureData
						  where measure.HierarchyId == id
						  select md.Id).Any();
			if (exists) {
				throw new Exception(Resource.HIERARCHY_ERR_DELETE);
			}
			else {
				// Find measures and Targets
				var measures = _dbc.Measure.Where(m => m.HierarchyId == id).ToArray();
				var targets = _dbc.Target.Where(t => measures.Any(m => m.Id == t.MeasureId)).ToArray();

				// delete Targets
				if (targets.Length > 0) {
					foreach (var item in targets) {
						_dbc.Target.Remove(item);
					}

					_dbc.SaveChanges();
				}

				// delete Measures
				if (measures.Length > 0) {
					foreach (var item in measures) {
						_dbc.Measure.Remove(item);
					}

					_dbc.SaveChanges();
				}

				// Find Hierarchy Name before deletion
				hierarchyName = _dbc.Hierarchy.Where(h => h.Id == id).First().Name;

				// delete Hierarchy
				_ = _dbc.Hierarchy.Remove(new() { Id = id });
				_dbc.SaveChanges();
			}

			Helper.AddAuditTrail(_dbc,
				Resource.WEB_PAGES,
				"WEB-05",
				Resource.HIERARCHY,
				@"Deleted / ID=" + id.ToString() +
						" / Name=" + hierarchyName,
				DateTime.Now,
				_user.Id
			);

			var regions = _dbc.Hierarchy.OrderBy(r => r.Id).ToArray();
			returnObject.Hierarchy.Add(new() {
				Hierarchy = regions.First().Name,
				Id = regions.First().Id,
				Sub = Helper.GetSubsAll(_dbc, regions.First().Id),
				Count = 0
			});
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_dbc, e, _user.Id)); ;
		}
	}

	class RegionParentChildPair
	{
		public RegionFilterObject? dto;
		public RegionParentChildPair? parent;
	}

	/// <summary>
	/// Return a hierarchy containing the hierarchies associated with <paramref name="userId"/>,
	/// plus all their ancestor hierarchies even if not associated with <paramref name="userId"/>.
	/// </summary>
	/// <param name="dbc"></param>
	/// <param name="userId"></param>
	/// <returns></returns>
	[NonAction]
	public static RegionFilterObject CreateUserHierarchy(ApplicationDbContext dbc, int userId) {
		// recursive CTE query
		var hierarchies = dbc.Hierarchy.FromSql($@"WITH p AS
(SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed
FROM Hierarchy WHERE HierarchyLevelId = 1 AND HierarchyParentId IS NULL
UNION ALL
SELECT ch.Id, ch.HierarchyLevelId, ch.HierarchyParentId, ch.[Name], ch.Active, ch.LastUpdatedOn, ch.IsProcessed
FROM Hierarchy ch JOIN p ON ch.HierarchyParentId = p.Id
WHERE ch.Active = 1)
SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed FROM p")
			.AsEnumerable()
			.Select(entity => new {
				entity,
				dto = new RegionFilterObject { Hierarchy = entity.Name, Id = entity.Id }
			})
			.ToArray();
		var root = hierarchies[0].dto;

		// create hierarchical doubly-linked list populated with parent and child references
		var pairs = Enumerable.Range(0, hierarchies.Length).Select(i => new RegionParentChildPair()).ToArray();
		for (var i = 0; i < hierarchies.Length; i++) {
			var pair = pairs[i];
			pairs[i].dto = hierarchies[i].dto;
			pairs[i].dto!.Sub = new List<RegionFilterObject>();
			for (var j = 0; j < pairs.Length; j++) {
				if (hierarchies[j].entity.HierarchyParentId == pairs[i].dto!.Id) {
					pairs[i].dto!.Sub.Add(hierarchies[j].dto);
					if (pairs[j] is null) {
						pairs[j] = new RegionParentChildPair { parent = pairs[i] };
					}
					else {
						pairs[j].parent = pairs[i];
					}
				}
			}
		}

		// must query UserHierarchy separately since CTEs are not EF-composable
		var userHierarchy = dbc.UserHierarchy
			.Where(u => u.UserId == userId)
			.Select(u => u.HierarchyId)
			.ToArray();

		// find pairs matching the user id
		var matched = new HashSet<RegionFilterObject> { root };
		foreach (var pair in pairs) {
			if (pair.dto!.Sub.Count == 0 && userHierarchy.Contains(pair.dto.Id)) {
				var found = pair;
				while (found!.parent is not null) {
					matched.Add(found.dto!);
					found = found.parent;
				}
			}
		}

		// prune children of non-matching nodes
		foreach (var node in matched) {
			node.Sub = node.Sub.Where(h => matched.Contains(h)).ToArray();
		}

		return root;
	}
}
