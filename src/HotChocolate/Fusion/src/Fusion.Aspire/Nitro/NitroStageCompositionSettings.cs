using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The composition settings that a Nitro stage declares. A value that the stage does not declare
/// is <c>null</c>.
/// </summary>
internal sealed record NitroStageCompositionSettings
{
    /// <summary>
    /// Gets the merge behavior for the cache control directive.
    /// </summary>
    public DirectiveMergeBehavior? CacheControlMergeBehavior { get; init; }

    /// <summary>
    /// Gets a value indicating whether global object identification is enabled.
    /// </summary>
    public bool? EnableGlobalObjectIdentification { get; init; }

    /// <summary>
    /// Gets the tags whose schema elements are excluded before the source schemas are merged.
    /// </summary>
    public IReadOnlyList<string>? ExcludeByTag { get; init; }

    /// <summary>
    /// Gets the schema that resolves the node field.
    /// </summary>
    public NodeResolution? NodeResolution { get; init; }

    /// <summary>
    /// Gets a value indicating whether definitions that no schema element references are removed.
    /// </summary>
    public bool? RemoveUnreferencedDefinitions { get; init; }

    /// <summary>
    /// Gets the merge behavior for the tag directive.
    /// </summary>
    public DirectiveMergeBehavior? TagMergeBehavior { get; init; }

    /// <summary>
    /// Creates the composition settings that these stage settings describe.
    /// </summary>
    public CompositionSettings ToCompositionSettings()
        => new()
        {
            Merger = new CompositionSettings.MergerSettings
            {
                CacheControlMergeBehavior = CacheControlMergeBehavior,
                EnableGlobalObjectIdentification = EnableGlobalObjectIdentification,
                NodeResolution = NodeResolution,
                RemoveUnreferencedDefinitions = RemoveUnreferencedDefinitions,
                TagMergeBehavior = TagMergeBehavior
            },
            Preprocessor = new CompositionSettings.PreprocessorSettings
            {
                ExcludeByTag = ExcludeByTag is null ? null : [.. ExcludeByTag]
            }
        };
}
