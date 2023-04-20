namespace Deliver.WebApi;

public sealed class AggregationFunction
{
    public byte Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public static class AggregationFunctions
{
    public static List<AggregationFunction> List { get; set; } = new()
    {
          new AggregationFunction { Id = 0, Name = "Summation" },
          new AggregationFunction { Id = 1, Name = "Average" },
          new AggregationFunction { Id = 2, Name = "MIN" },
          new AggregationFunction { Id = 3, Name = "MAX" },
          new AggregationFunction { Id = 4, Name = "Standard Deviation" },
          new AggregationFunction { Id = 5, Name = "Expression" }
    };
}

public enum EnumAggFunctions { summation = 0, average = 1, min = 2, max = 3, standardDeviation = 4, expression = 5 };
