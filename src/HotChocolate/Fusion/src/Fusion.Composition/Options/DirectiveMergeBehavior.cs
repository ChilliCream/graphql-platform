namespace HotChocolate.Fusion.Options;

/// <summary>
/// Defines how to handle directives when merging source schemas.
/// </summary>
public enum DirectiveMergeBehavior
{
    /// <summary>
    /// Ignore the directives.
    /// </summary>
    Ignore = 0,
    /// <summary>
    /// Merge the directives publicly (<c>@foo</c>).
    /// </summary>
    Include = 1,
    /// <summary>
    /// Merge the directives privately (<c>@fusion__foo</c>).
    /// </summary>
    IncludePrivate = 2
}
