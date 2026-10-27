using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// A schema, operation, and variable set for testing custom analyses.
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
