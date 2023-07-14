using System.Text.Json;

namespace HotChocolate.Fusion.Composition.Features;

public sealed class NodeFieldFeature : IFusionFeature, IFusionFeatureParser<NodeFieldFeature>
{
    private NodeFieldFeature() { }

    public static NodeFieldFeature Instance { get; } = new();

    public static NodeFieldFeature Parse(JsonElement value)
    {
        if (value.TryGetProperty("type", out var type) && type.GetString() == "nodeFieldSupport")
        {
            return Instance;
        }

        throw new InvalidOperationException("The value is not a node field feature configuration section.");
    }
}