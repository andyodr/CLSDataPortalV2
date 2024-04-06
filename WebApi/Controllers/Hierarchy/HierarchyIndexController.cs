using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Hierarchy;

[ApiController]
[Route("api/hierarchy/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public sealed class IndexController : BaseController
{
	[HttpGet]
	public ActionResult<RegionMetricsFilterObject> Get() {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			RegionMetricsFilterObject result = new() {
				Data = Dbc.Hierarchy
					.Select(h => new RegionsDataViewModel {
						Id = h.Id,
						Name = h.Name,
						LevelId = h.HierarchyLevelId,
						Level = h.HierarchyLevel!.Name,
						Active = h.Active ?? false,
						Remove = !Dbc.Measure.Where(m => m.HierarchyId == h.Id && m.MeasureData.Count != 0).Any(),
						ParentId = h.HierarchyParentId,
						ParentName = h.Parent == null ? "" : h.Parent.Name
					}).ToArray().OrderByHierarchy(),
				Hierarchy = [CreateHierarchy(Dbc)],
				Levels = [.. Dbc.HierarchyLevel.OrderBy(l => l.Id).Select(l => new LevelObject { Id = l.Id, Name = l.Name })]
			};

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}

	}

	[HttpPost]
	public ActionResult<RegionMetricsFilterObject> Post(RegionsDataViewModelAdd body) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var newHierarchy = Dbc.Hierarchy.Add(new() {
				Name = body.Name,
				HierarchyParentId = body.ParentId,
				HierarchyLevelId = body.LevelId,
				Active = body.Active,
				IsProcessed = (byte)IsProcessed.Complete,
				LastUpdatedOn = DateTime.Now
			}).Entity;
			_ = Dbc.SaveChanges();
			RegionsDataViewModel dto = new() {
				Id = newHierarchy.Id,
				Name = newHierarchy.Name,
				LevelId = newHierarchy.HierarchyLevelId,
				Level = Dbc.HierarchyLevel.Where(h => h.Id == newHierarchy.HierarchyLevelId).First().Name,
				ParentId = newHierarchy.HierarchyParentId,
				Active = newHierarchy.Active ?? false
			};

			var parent = Dbc.Hierarchy.Where(h => h.Id == newHierarchy.HierarchyParentId).FirstOrDefault();
			dto.ParentId = parent?.Id;
			dto.ParentName = parent?.Name ?? string.Empty;

			CreateMeasuresAndTargets(_user.Id, dto.Id);

			_ = Dbc.SaveChanges();
			var exists = (from measure in Dbc.Measure
						  from md in measure.MeasureData
						  where measure.HierarchyId == dto.Id
						  select md.Id).Any();
			dto.Remove = !exists;
			RegionMetricsFilterObject result = new() {
				Data = [dto],
				Hierarchy = [CreateHierarchy(Dbc)]
			};

			Dbc.AddAuditTrail(Resource.WEB_PAGES, "WEB-05",
				Resource.HIERARCHY,
				@"Added / ID=" + dto.Id.ToString(),
				DateTime.Now,
				_user.Id
			);

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}

	[HttpPut]
	public ActionResult<RegionMetricsFilterObject> Put(RegionsDataViewModel dto) {
		var result = new RegionMetricsFilterObject { Data = [] };
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var updateHierarchy = Dbc.Hierarchy.Find(dto.Id);
			if (updateHierarchy is null) {
				return result;
			}

			DateTime updatedOn = DateTime.Now;
			updateHierarchy.Name = dto.Name;
			updateHierarchy.Active = dto.Active;
			updateHierarchy.HierarchyParentId = dto.ParentId;
			updateHierarchy.LastUpdatedOn = updatedOn;
			updateHierarchy.HierarchyLevelId = dto.LevelId;
			updateHierarchy.IsProcessed = (byte)IsProcessed.Complete;
			Dbc.SaveChanges();

			RegionsDataViewModel newHierarchy = new() {
				Id = updateHierarchy.Id,
				Name = updateHierarchy.Name,
				LevelId = updateHierarchy.HierarchyLevelId,
				Active = updateHierarchy.Active ?? false,
				Level = Dbc.HierarchyLevel.Where(h => h.Id == updateHierarchy.HierarchyLevelId).First().Name
			};
			var exists = (from measure in Dbc.Measure
						  from md in measure.MeasureData
						  where measure.HierarchyId == newHierarchy.Id
						  select md.Id).Any();
			newHierarchy.Remove = !exists;
			var parent = Dbc.Hierarchy.Where(h => h.Id == dto.ParentId).FirstOrDefault();
			newHierarchy.ParentId = parent?.Id;
			newHierarchy.ParentName = parent?.Name ?? string.Empty;

			Dbc.AddAuditTrail(Resource.WEB_PAGES, "WEB-05",
				Resource.HIERARCHY,
				@"Updated / ID=" + newHierarchy.Id.ToString(),
				updatedOn,
				_user.Id
			);

			result.Data.Add(newHierarchy);
			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}

	[HttpDelete("{id}")]
	public ActionResult<RegionMetricsFilterObject> Delete(int id) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var result = new RegionMetricsFilterObject {
				Data = [],
				Hierarchy = [CreateHierarchy(Dbc)]
			};
			string hierarchyName = string.Empty;
			var exists = (from measure in Dbc.Measure
						  from md in measure.MeasureData
						  where measure.HierarchyId == id
						  select md.Id).Any();
			if (exists) {
				throw new Exception(Resource.HIERARCHY_ERR_DELETE);
			}
			else {
				// Find measures and Targets
				var measures = Dbc.Measure.Where(m => m.HierarchyId == id).ToArray();
				var targets = Dbc.Target.Where(t => measures.Any(m => m.Id == t.MeasureId)).ToArray();

				// delete Targets
				if (targets.Length > 0) {
					foreach (var item in targets) {
						Dbc.Target.Remove(item);
					}

					Dbc.SaveChanges();
				}

				// delete Measures
				if (measures.Length > 0) {
					foreach (var item in measures) {
						Dbc.Measure.Remove(item);
					}

					Dbc.SaveChanges();
				}

				// Find Hierarchy Name before deletion
				hierarchyName = Dbc.Hierarchy.Where(h => h.Id == id).First().Name;

				// delete Hierarchy
				_ = Dbc.Hierarchy.Remove(new() { Id = id });
				Dbc.SaveChanges();
			}

			Dbc.AddAuditTrail(Resource.WEB_PAGES, "WEB-05",
				Resource.HIERARCHY,
				@"Deleted / ID=" + id.ToString() +
						" / Name=" + hierarchyName,
				DateTime.Now,
				_user.Id
			);

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id)); ;
		}
	}

	private void CreateMeasuresAndTargets(int userId, int hierarchyId) {
		var dtNow = DateTime.Now;
		//create Measure records
		Dbc.AddRange(Dbc.MeasureDefinition.Select(df => new Data.Models.Target() {
			Active = true,
			UserId = userId,
			IsProcessed = (byte)IsProcessed.Complete,
			Measure = new() {
				HierarchyId = hierarchyId,
				MeasureDefinitionId = df.Id,
				Active = true,
				Expression = df.Calculated,
				Rollup = true,
				LastUpdatedOn = dtNow
			},
			LastUpdatedOn = dtNow
		}));
	}

	class HierarchyNode
	{
		public RegionFilterObject? dto;
		public HierarchyNode? parent;
	}

	private static RegionFilterObject CreateHierarchy(ApplicationDbContext dbc) {
		// recursive CTE query
		var hierarchies = dbc.Hierarchy.FromSql($"""
			WITH r AS
				(SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed
				FROM Hierarchy WHERE HierarchyParentId IS NULL
				UNION ALL
				SELECT ch.Id, ch.HierarchyLevelId, ch.HierarchyParentId, ch.[Name], ch.Active, ch.LastUpdatedOn, ch.IsProcessed
				FROM Hierarchy ch JOIN r ON ch.HierarchyParentId = r.Id)
			SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed FROM r
			""")
			.AsEnumerable()
			.Select(entity => new {
				entity,
				dto = new RegionFilterObject { Hierarchy = entity.Name, Id = entity.Id }
			})
			.ToArray();
		var root = hierarchies[0].dto;

		// doubly-linked list constructing the parent/child hierarchy in hierarchies[]
		var nodes = Enumerable.Range(0, hierarchies.Length).Select(i => new HierarchyNode()).ToArray();
		for (var i = 0; i < hierarchies.Length; i++) {
			var node = nodes[i];
			node.dto = hierarchies[i].dto;
			node.dto!.Sub = [];
			for (var j = 0; j < nodes.Length; j++) {
				if (hierarchies[j].entity.HierarchyParentId == node.dto!.Id) {
					node.dto!.Sub.Add(hierarchies[j].dto);
					if (nodes[j] is null) {
						nodes[j] = new HierarchyNode { parent = node };
					}
					else {
						nodes[j].parent = node;
					}
				}
			}
		}

		return root;
	}

	/// <returns>
	/// Rooted hierarchy containing the hierarchies associated with <paramref name="userId"/>
	/// </returns>
	[NonAction]
	public static RegionFilterObject CreateUserHierarchy(ApplicationDbContext dbc, int userId) {
		// recursive CTE query
		var hierarchies = dbc.Hierarchy.FromSql($"""
			WITH cte AS
				(SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed
				FROM Hierarchy WHERE HierarchyParentId IS NULL
				UNION ALL
				SELECT ch.Id, ch.HierarchyLevelId, ch.HierarchyParentId, ch.[Name], ch.Active, ch.LastUpdatedOn, ch.IsProcessed
				FROM Hierarchy ch JOIN cte ON ch.HierarchyParentId = cte.Id
				WHERE ch.Active = 1)
			SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed FROM cte
			""")
			.AsEnumerable()
			.Select(entity => new {
				entity,
				dto = new RegionFilterObject { Hierarchy = entity.Name, Id = entity.Id }
			})
			.ToArray();
		var root = hierarchies[0].dto;

		// create doubly-linked list representing the parent/child hierarchy
		var nodes = Enumerable.Range(0, hierarchies.Length).Select(i => new HierarchyNode()).ToArray();
		for (var i = 0; i < hierarchies.Length; i++) {
			var node = nodes[i];
			node.dto = hierarchies[i].dto;
			node.dto!.Sub = [];
			for (var j = 0; j < nodes.Length; j++) {
				if (hierarchies[j].entity.HierarchyParentId == node.dto!.Id) {
					node.dto!.Sub.Add(hierarchies[j].dto);
					if (nodes[j] is null) {
						nodes[j] = new HierarchyNode { parent = node };
					}
					else {
						nodes[j].parent = node;
					}
				}
			}
		}

		// CTEs are not EF-composable so this is why we need two separate queries to build the hierarchy
		var userHierarchy = dbc.UserHierarchy
			.Where(u => u.UserId == userId)
			.Select(u => u.HierarchyId)
			.ToArray();

		// find pairs matching the user id
		var matched = new HashSet<RegionFilterObject>();
		foreach (var pair in nodes) {
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
			node.Sub = node.Sub.Where(matched.Contains).ToArray();
		}

		return root;
	}
}
