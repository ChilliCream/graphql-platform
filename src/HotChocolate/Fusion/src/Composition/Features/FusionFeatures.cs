namespace HotChocolate.Fusion.Composition.Features;

public static class FusionFeatures
{
    public static NodeFieldFeature NodeField => NodeFieldFeature.Instance;

    public static ReEncodeIdsFeature ReEncodeIds => ReEncodeIdsFeature.Instance;

    public static TagDirectiveFeature TagDirective(
        IEnumerable<string>? exclude = null,
        bool makeTagsPublic = false) 
        => new(exclude, makeTagsPublic);
}