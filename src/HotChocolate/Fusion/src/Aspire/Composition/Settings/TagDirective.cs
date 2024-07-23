using System.Text.Json.Serialization;

namespace HotChocolate.Fusion.Composition.Settings;

internal sealed class TagDirective : Feature
{
    private string[]? _exclude;

    [JsonPropertyName("makePublic")]
    [JsonPropertyOrder(100)]
    public bool MakePublic { get; set; }

    [JsonPropertyName("exclude")]
    [JsonPropertyOrder(101)]
    public string[] Exclude
    {
        get => _exclude ?? [];
        set => _exclude = value;
    }
}
