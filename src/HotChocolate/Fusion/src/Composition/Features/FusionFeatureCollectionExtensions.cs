namespace HotChocolate.Fusion.Composition.Features;

/// <summary>
/// Provides aggregations and helpers to inspect the <see cref="FusionFeatureCollection"/>.
/// </summary>
public static class FusionFeatureCollectionExtensions
{
    private static readonly HashSet<string> _empty = [];

    /// <summary>
    /// Specifies if the fusion graph shall support the global identification spec.
    /// </summary>
    /// <param name="features">
    /// The feature collection.
    /// </param>
    /// <returns>
    /// <c>true</c> if the global identification spec is supported; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNodeFieldSupported(this FusionFeatureCollection features)
        => features.IsSupported<NodeFieldFeature>();

    /// <summary>
    /// Specifies if the @tag directive shall be exposed publicly.
    /// </summary>
    /// <param name="features">
    /// The feature collection.
    /// </param>
    /// <returns>
    /// <c>true</c> if the @tag directive shall be exposed publicly; otherwise, <c>false</c>.
    /// </returns>
    public static bool MakeTagsPublic(this FusionFeatureCollection features)
        => features.TryGetFeature<TagDirectiveFeature>(out var feature) &&
            feature.MakeTagsPublic;

    /// <summary>
    /// Gets the tags that shall be used to exclude parts of the subgraph schemas.
    /// </summary>
    /// <param name="features">
    /// The feature collection.
    /// </param>
    /// <returns>
    /// The tags that shall be used to exclude parts of the subgraph schemas.
    /// </returns>
    public static IReadOnlySet<string> GetExcludedTags(this FusionFeatureCollection features)
        => features.TryGetFeature<TagDirectiveFeature>(out var feature)
            ? feature.Excluded
            : _empty;

    /// <summary>
    /// Gets the default client configuration name that shall be used for
    /// transport clients if no client name was specified.
    /// </summary>
    /// <param name="features">
    /// The feature collection.
    /// </param>
    /// <returns>
    /// The default client configuration name.
    /// </returns>
    public static string? GetDefaultClientName(this FusionFeatureCollection features)
        => features.TryGetFeature<TransportFeature>(out var feature)
            ? feature.DefaultClientName
            : null;
}
