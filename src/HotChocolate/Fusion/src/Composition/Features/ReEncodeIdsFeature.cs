using System.Text.Json;

namespace HotChocolate.Fusion.Composition.Features;

public sealed class ReEncodeIdsFeature : IFusionFeature, IFusionFeatureParser<ReEncodeIdsFeature>
{
    private ReEncodeIdsFeature() { }

    public static ReEncodeIdsFeature Instance { get; } = new();

    public static ReEncodeIdsFeature Parse(JsonElement value)
    {
        if (value.TryGetProperty("type", out var type) && type.GetString() == "reEncodeIds")
        {
            return Instance;
        }

        throw new InvalidOperationException("The value is not a reEncodeIds feature configuration section.");
    }
}