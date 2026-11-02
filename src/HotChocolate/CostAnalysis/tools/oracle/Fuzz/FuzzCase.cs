using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotChocolate.CostAnalysis.Fuzz;

internal sealed record FuzzCase(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("seed")] int Seed,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("sdl")] string Sdl,
    [property: JsonPropertyName("operation")] string Operation,
    [property: JsonPropertyName("oracleOperation")] string OracleOperation,
    [property: JsonPropertyName("operationName")] string OperationName,
    [property: JsonPropertyName("variables")] JsonElement Variables,
    [property: JsonPropertyName("defaultListSize")] int DefaultListSize,
    [property: JsonPropertyName("expected")] FuzzCost Expected);

internal sealed record FuzzCost(
    [property: JsonPropertyName("typeCost")] double TypeCost,
    [property: JsonPropertyName("fieldCost")] double FieldCost,
    [property: JsonPropertyName("typeCostBits")] string TypeCostBits,
    [property: JsonPropertyName("fieldCostBits")] string FieldCostBits)
{
    public static FuzzCost From(double typeCost, double fieldCost)
        => new(typeCost, fieldCost, Bits(typeCost), Bits(fieldCost));

    private static string Bits(double value)
        => unchecked((ulong)BitConverter.DoubleToInt64Bits(value)).ToString("x16");
}

internal sealed record OracleResult(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("typeCost")] double TypeCost,
    [property: JsonPropertyName("fieldCost")] double FieldCost,
    [property: JsonPropertyName("typeCostBits")] string TypeCostBits,
    [property: JsonPropertyName("fieldCostBits")] string FieldCostBits);
