namespace HotChocolate.Fusion.Composition.Features;

/// <summary>
/// Represents the available Fusion composition features.
/// </summary>
public static class FusionFeatures
{
    /// <summary>
    /// Specifies if the fusion graph shall support the global identification spec.
    /// </summary>
    public static NodeFieldFeature NodeField => NodeFieldFeature.Instance;

    /// <summary>
    /// Specifies if the fusion graph shall re-encode the ids of subgraph schemas.
    /// </summary>
    public static ReEncodeIdsFeature ReEncodeIds => ReEncodeIdsFeature.Instance;

    /// <summary>
    /// Specifies behavior of the @tag directive.
    /// </summary>
    /// <param name="exclude">
    /// The tags that shall be used to exclude parts of the subgraph schemas.
    /// </param>
    /// <param name="makeTagsPublic">
    /// Specifies if the @tag directive shall be exposed publicly.
    /// </param>
    /// <returns>
    /// The @tag directive feature.
    /// </returns>
    public static TagDirectiveFeature TagDirective(
        IEnumerable<string>? exclude = null,
        bool makeTagsPublic = false)
        => new(exclude, makeTagsPublic);
}
