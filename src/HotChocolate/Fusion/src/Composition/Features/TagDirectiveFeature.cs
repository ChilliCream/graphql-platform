namespace HotChocolate.Fusion.Composition.Features;

/// <summary>
/// Specifies behavior of the @tag directive.
/// </summary>
public sealed class TagDirectiveFeature : IFusionFeature
{
    public TagDirectiveFeature(
        IEnumerable<string>? exclude = null,
        bool makeTagsPublic = false)
    {
        Excluded = new HashSet<string>(exclude ?? Enumerable.Empty<string>());
        MakeTagsPublic = makeTagsPublic;
    }

    /// <summary>
    /// Gets the tags that shall be excluded from the public schema.
    /// </summary>
    public IReadOnlySet<string> Excluded { get; }

    /// <summary>
    /// Defines if the tag directives should be exported to the public schema.
    /// </summary>
    public bool MakeTagsPublic { get; }
}