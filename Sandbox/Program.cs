// See https://aka.ms/new-console-template for more information
using CLS.WebApi.Data;
using ConsoleApp1;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

Console.WriteLine("Hello, World!");
JsonSerializerOptions webStandard = new() {
	PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	ReferenceHandler = ReferenceHandler.IgnoreCycles,
	WriteIndented = true
};

using (var dbc = new SandboxDbContext()) {
	var measures = dbc.Measure.Where(m => m.Active == true && m.Hierarchy!.Name == "PacRim").ToList();
	Console.WriteLine(measures);

	var mdef = dbc.MeasureDefinition
		.Include(d => d.ReportInterval)
		.Where(md => md.Id == 30)
		.AsNoTracking().First();
	Console.WriteLine($"MeasureDefinition.ReportIntervalId = {mdef.ReportInterval!.Id}");

	bool? x = null;
	bool? y = true;
	bool? z = false;
	Console.WriteLine($"!x -> {(!x == null ? "is null" : !x)}");
	Console.WriteLine($"!y -> {(!y == null ? "is null" : !y)}");
	Console.WriteLine($"!z -> {(!z == null ? "is null" : !z)}");
	var qq = dbc.MeasureType.FirstOrDefault(q => q.Name.Contains("qq"));
	Console.WriteLine($"FirstOrDefault -> {qq?.ToString() ?? "is null"}");

	// heierarchy table currently contains 5 levels of nesting
	var hierarchy = from h in dbc.Hierarchy.Include(h => h.HierarchyLevel)
					.Include(h => h.Children)!.ThenInclude(h => h.HierarchyLevel)
					.Include(h => h.Children)!.ThenInclude(h => h.Children)!.ThenInclude(h => h.HierarchyLevel)
					.Include(h => h.Children)!.ThenInclude(h => h.Children)!.ThenInclude(h => h.Children)!.ThenInclude(h => h.HierarchyLevel)
					.Include(h => h.Children)!.ThenInclude(h => h.Children)!.ThenInclude(h => h.Children)!.ThenInclude(h => h.Children)!.ThenInclude(h => h.HierarchyLevel)
					where h.Active == true && h.Id == 1
					select h;
	//var f = hierarchy.First();
	//dbc.Entry(f).Collection(f => f.Children!).Load();  // load a navigation property for just one entity
	//dbc.Entry(f).Collection(f => f.Children!).Query(); // get a query for just one entity
	Console.WriteLine(JsonSerializer.Serialize(hierarchy.ToList(), webStandard));
}

var node = JsonNode.Parse("[200]");
switch (node) {
	case JsonArray:
		Console.WriteLine($"JsonArray -> {node[0]}");
		break;
	case JsonValue:
		Console.WriteLine($"JsonValue -> {node!.AsValue().GetValue<int>()}");
		break;
	case JsonObject:
		Console.WriteLine($"JsonObject");
		break;
}

CalendarLock c = new();
var json = JsonSerializer.Serialize(c, webStandard);
Console.WriteLine(json);
