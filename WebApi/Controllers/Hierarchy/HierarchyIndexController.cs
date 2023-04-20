using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Hierarchy;

[ApiController]
[Route("api/hierarchy/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public sealed class IndexController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;

	public IndexController(ApplicationDbContext context) => _dbc = context;

	[HttpGet]
	public ActionResult<RegionMetricsFilterObject> Get() {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var result = new RegionMetricsFilterObject {
				Data = _dbc.Hierarchy
					.Select(h => new RegionsDataViewModel {
						Id = h.Id,
						Name = h.Name,
						LevelId = h.HierarchyLevelId,
						Level = h.HierarchyLevel!.Name,
						Active = h.Active ?? false,
						Remove = !_dbc.Measure.Where(m => m.HierarchyId == h.Id && m.MeasureData.Any()).Any(),
						ParentId = h.HierarchyParentId,
						ParentName = h.Parent == null ? "" : h.Parent.Name
					})
					.ToArray(),
				Hierarchy = new RegionFilterObject[] { CreateHierarchy(_dbc) },
				Levels = _dbc.HierarchyLevel.OrderBy(l => l.Id)
					.Select(l => new LevelObject { Id = l.Id, Name = l.Name })
					.ToArray() };

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}

	}

	[HttpPost]
	public ActionResult<RegionMetricsFilterObject> Post(RegionsDataViewModelAdd dto) {
		var result = new RegionMetricsFilterObject {
			Data = new List<RegionsDataViewModel>(),
			Hierarchy = new RegionFilterObject[] { CreateHierarchy(_dbc) }
		};
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var updatedHierarchy = _dbc.Hierarchy.Add(new() {
				Name = dto.Name,
				HierarchyParentId = dto.ParentId,
				HierarchyLevelId = dto.LevelId,
				Active = dto.Active,
				IsProcessed = 2,
				LastUpdatedOn = DateTime.Now
			}).Entity;
			_ = _dbc.SaveChanges();
			RegionsDataViewModel newHierarchy = new() {
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

			string creationResult = CreateMeasuresAndTargets(_user.Id, newHierarchy.Id);
			if (!string.IsNullOrEmpty(creationResult)) {
				throw new Exception(creationResult);
			}

			_dbc.SaveChanges();
			var exists = (from measure in _dbc.Measure
						  from md in measure.MeasureData
						  where measure.HierarchyId == newHierarchy.Id
						  select md.Id).Any();
			newHierarchy.Remove = !exists;
			result.Data.Add(newHierarchy);

			AddAuditTrail(_dbc,
				Resource.WEB_PAGES,
				"WEB-05",
				Resource.HIERARCHY,
				@"Added / ID=" + newHierarchy.Id.ToString(),
				DateTime.Now,
				_user.Id
			);

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}

	[HttpPut]
	public ActionResult<RegionMetricsFilterObject> Put(RegionsDataViewModel dto) {
		var result = new RegionMetricsFilterObject { Data = new List<RegionsDataViewModel>() };
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			DateTime updatedOn = DateTime.Now;
			var updateHierarchy = _dbc.Hierarchy.Find(dto.Id);
			if (updateHierarchy is null) {
				return result;
			}

			updateHierarchy.Name = dto.Name;
			updateHierarchy.Active = dto.Active;
			updateHierarchy.HierarchyParentId = dto.ParentId;
			updateHierarchy.LastUpdatedOn = updatedOn;
			updateHierarchy.HierarchyLevelId = dto.LevelId;
			if (updateHierarchy.HierarchyLevelId == hierarchyGlobalId) {
				updateHierarchy.HierarchyParentId = null;
			}

			updateHierarchy.IsProcessed = (byte)IsProcessed.Complete;
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

			AddAuditTrail(_dbc,
				Resource.WEB_PAGES,
				"WEB-05",
				Resource.HIERARCHY,
				@"Updated / ID=" + newHierarchy.Id.ToString(),
				updatedOn,
				_user.Id
			);

			result.Data.Add(newHierarchy);
			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}

	[HttpDelete("{id}")]
	public ActionResult<RegionMetricsFilterObject> Delete(int id) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var result = new RegionMetricsFilterObject {
				Data = new List<RegionsDataViewModel>(),
				Hierarchy = new RegionFilterObject[] { CreateHierarchy(_dbc) }
			};
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

			AddAuditTrail(_dbc,
				Resource.WEB_PAGES,
				"WEB-05",
				Resource.HIERARCHY,
				@"Deleted / ID=" + id.ToString() +
						" / Name=" + hierarchyName,
				DateTime.Now,
				_user.Id
			);

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id)); ;
		}
	}

	private string CreateMeasuresAndTargets(int userId, int hierarchyId) {
		try {
			string result = string.Empty;
			var dtNow = DateTime.Now;
			foreach (var measureDef in _dbc.MeasureDefinition.Select(md => new { md.Id, md.Calculated })) {
				//create Measure records
				_ = _dbc.Measure.Add(new() {
					HierarchyId = hierarchyId,
					MeasureDefinitionId = measureDef.Id,
					Active = true,
					Expression = measureDef.Calculated,
					Rollup = true,
					LastUpdatedOn = dtNow
				});
			}

			//make target ids
			foreach (var measure in _dbc.Measure.Where(m => m.HierarchyId == hierarchyId)) {
				_ = _dbc.Target.Add(new() {
					Measure = measure,
					Active = true,
					UserId = userId,
					IsProcessed = (byte)IsProcessed.Complete,
					LastUpdatedOn = dtNow
				});
			}

			return result;
		}
		catch (Exception e) {
			return e.Message;
		}
	}

	class RegionParentChildPair
	{
		public RegionFilterObject? dto;
		public RegionParentChildPair? parent;
	}

	private static RegionFilterObject CreateHierarchy(ApplicationDbContext dbc) {
		// recursive CTE query
		var hierarchies = dbc.Hierarchy.FromSql($@"WITH r AS
(SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed
FROM Hierarchy WHERE HierarchyParentId IS NULL
UNION ALL
SELECT ch.Id, ch.HierarchyLevelId, ch.HierarchyParentId, ch.[Name], ch.Active, ch.LastUpdatedOn, ch.IsProcessed
FROM Hierarchy ch JOIN r ON ch.HierarchyParentId = r.Id)
SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed FROM r")
			.AsEnumerable()
			.Select(entity => new {
				entity,
				dto = new RegionFilterObject { Hierarchy = entity.Name, Id = entity.Id }
			})
			.ToArray();
		var root = hierarchies[0].dto;

		// doubly-linked list constructing the parent/child hierarchy in hierarchies[]
		var pairs = Enumerable.Range(0, hierarchies.Length).Select(i => new RegionParentChildPair()).ToArray();
		for (var i = 0; i < hierarchies.Length; i++) {
			var pair = pairs[i];
			pair.dto = hierarchies[i].dto;
			pair.dto!.Sub = new List<RegionFilterObject>();
			for (var j = 0; j < pairs.Length; j++) {
				if (hierarchies[j].entity.HierarchyParentId == pair.dto!.Id) {
					pair.dto!.Sub.Add(hierarchies[j].dto);
					if (pairs[j] is null) {
						pairs[j] = new RegionParentChildPair { parent = pair };
					}
					else {
						pairs[j].parent = pair;
					}
				}
			}
		}

		return root;
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
		var hierarchies = dbc.Hierarchy.FromSql($@"WITH r AS
(SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed
FROM Hierarchy WHERE HierarchyParentId IS NULL
UNION ALL
SELECT ch.Id, ch.HierarchyLevelId, ch.HierarchyParentId, ch.[Name], ch.Active, ch.LastUpdatedOn, ch.IsProcessed
FROM Hierarchy ch JOIN r ON ch.HierarchyParentId = r.Id
WHERE ch.Active = 1)
SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed FROM r")
			.AsEnumerable()
			.Select(entity => new {
				entity,
				dto = new RegionFilterObject { Hierarchy = entity.Name, Id = entity.Id }
			})
			.ToArray();
		var root = hierarchies[0].dto;

		// create doubly-linked list representing the parent/child hierarchy
		var pairs = Enumerable.Range(0, hierarchies.Length).Select(i => new RegionParentChildPair()).ToArray();
		for (var i = 0; i < hierarchies.Length; i++) {
			var pair = pairs[i];
			pair.dto = hierarchies[i].dto;
			pair.dto!.Sub = new List<RegionFilterObject>();
			for (var j = 0; j < pairs.Length; j++) {
				if (hierarchies[j].entity.HierarchyParentId == pair.dto!.Id) {
					pair.dto!.Sub.Add(hierarchies[j].dto);
					if (pairs[j] is null) {
						pairs[j] = new RegionParentChildPair { parent = pair };
					}
					else {
						pairs[j].parent = pair;
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
		var matched = new HashSet<RegionFilterObject>();
		foreach (var pair in pairs) {
			if (pair.dto!.Sub.Count == 0 && Array.Exists(userHierarchy, i => i == pair.dto.Id)
				|| Array.Exists(userHierarchy, i => i == pair.parent?.dto!.Id)) {
				var found = pair;
				while (found is not null) {
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
