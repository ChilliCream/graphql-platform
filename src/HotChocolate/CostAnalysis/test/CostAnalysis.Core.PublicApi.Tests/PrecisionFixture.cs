using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// The subset of a Conformance.Tests precision fixture this project needs:
/// enough to compile an <see cref="AnalysisPlan"/> and supply a fixed
/// variable set, without depending on the IBM-cost-specific
/// <c>expected</c> numbers those fixtures also carry.
/// </summary>
public sealed record PrecisionFixture(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("sdl")] string Sdl,
    [property: JsonPropertyName("operation")] string Operation,
    [property: JsonPropertyName("operationName")] string? OperationName,
    [property: JsonPropertyName("variables")] JsonElement? Variables)
{
    public static PrecisionFixture Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<PrecisionFixture>(json)
            ?? throw new InvalidOperationException($"Fixture '{path}' deserialized to null.");
    }
}
