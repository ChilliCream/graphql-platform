using System.Text.Json;

namespace HotChocolate.Fusion.Composition.Features;

public static class FusionFeatureCollectionExtensions
{
    public static bool IsNodeFieldSupported(this FusionFeatureCollection features)
        => features.IsSupported<NodeFieldFeature>();
    
    public static bool MakeTagsPublic(this FusionFeatureCollection features)
        => features.TryGetFeature<TagDirectiveFeature>(out var feature) &&
            feature.MakeTagsPublic;
    
    public static FusionFeatureCollection Parse(this JsonElement value)
    {
        var features = new List<IFusionFeature>();

        foreach (var feature in value.EnumerateArray())
        {
            if (feature.TryGetProperty("type", out var type))
            {
                if (type.GetString() == "nodeFieldSupport")
                {
                    features.Add(NodeFieldFeature.Parse(feature));
                }
                else if (type.GetString() == "reEncodeIds")
                {
                    features.Add(ReEncodeIdsFeature.Parse(feature));
                }
                else if (type.GetString() == "tagDirective")
                {
                    features.Add(TagDirectiveFeature.Parse(feature));
                }
            }
        }

        return new FusionFeatureCollection(features);
    }
}