namespace HotChocolate.Fusion.Composition.Features;

public sealed class TagDirectiveFeature : IFusionFeature
{
    public TagDirectiveFeature(
        IEnumerable<string>? exclude = null,
        bool makeTagsPublic = false)
    {
        Tags = new HashSet<string>(exclude ?? Enumerable.Empty<string>());
        MakeTagsPublic = makeTagsPublic;
    }

    public IReadOnlySet<string> Tags { get; }

    /// <summary>
    /// Defines if the tag directives should be exported to the public schema.
    /// </summary>
    public bool MakeTagsPublic { get; }
}