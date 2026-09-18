using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// One conformance fixture, deserialized from a file under
/// <c>__resources__</c> that matches <c>fixture.schema.json</c>.
/// </summary>
internal sealed record Fixture(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("sdl")] string Sdl,
    [property: JsonPropertyName("operation")] string Operation,
    [property: JsonPropertyName("operationName")] string? OperationName,
    [property: JsonPropertyName("variables")] JsonElement? Variables,
    [property: JsonPropertyName("defaultListSize")] JsonElement DefaultListSize,
    [property: JsonPropertyName("backend")] string Backend,
    [property: JsonPropertyName("expected")] FixtureExpected Expected,
    [property: JsonPropertyName("notes")] string? Notes)
{
    public static Fixture Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Fixture>(json)
            ?? throw new InvalidOperationException($"Fixture '{path}' deserialized to null.");
    }
}

/// <summary>
/// The expected IBM typeCost/fieldCost pair of a <see cref="Fixture"/>.
/// </summary>
internal sealed record FixtureExpected(
    [property: JsonPropertyName("typeCost")] double TypeCost,
    [property: JsonPropertyName("fieldCost")] double FieldCost);
