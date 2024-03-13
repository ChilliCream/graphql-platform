using System.Text.Json.Serialization;

namespace HotChocolate.Fusion.Composition;

internal sealed class Transport
{
    [JsonPropertyName("defaultClientName")]
    [JsonPropertyOrder(10)]
    public string? DefaultClientName { get; set; } = "Fusion";
}
