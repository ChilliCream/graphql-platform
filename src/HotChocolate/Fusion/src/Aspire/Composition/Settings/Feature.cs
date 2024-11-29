using System.Text.Json.Serialization;

namespace HotChocolate.Fusion.Composition.Settings;

internal class Feature
{
    [JsonPropertyName("enabled")]
    [JsonPropertyOrder(10)]
    public bool Enabled { get; set; }
}
