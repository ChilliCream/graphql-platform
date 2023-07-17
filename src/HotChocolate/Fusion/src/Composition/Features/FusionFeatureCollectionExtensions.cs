namespace HotChocolate.Fusion.Composition.Features;

public static class FusionFeatureCollectionExtensions
{
    public static bool IsNodeFieldSupported(this FusionFeatureCollection features)
        => features.IsSupported<NodeFieldFeature>();
    
    public static bool MakeTagsPublic(this FusionFeatureCollection features)
        => features.TryGetFeature<TagDirectiveFeature>(out var feature) &&
            feature.MakeTagsPublic;
}