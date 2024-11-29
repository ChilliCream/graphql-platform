namespace HotChocolate.Fusion.Composition.Features;

/// <summary>
/// Specifies behavior of the @tag directive.
/// </summary>
public sealed class TagDirectiveFeature(
    IEnumerable<string>? exclude = null,
    bool makeTagsPublic = false)
    : IFusionFeature
{
    /// <summary>
    /// Gets the tags that shall be excluded from the public schema.
    /// </summary>
    public IReadOnlySet<string> Excluded { get; } = new HashSet<string>(exclude ?? []);

    /// <summary>
    /// Defines if the tag directives should be exported to the public schema.
    /// </summary>
    public bool MakeTagsPublic { get; } = makeTagsPublic;
}
