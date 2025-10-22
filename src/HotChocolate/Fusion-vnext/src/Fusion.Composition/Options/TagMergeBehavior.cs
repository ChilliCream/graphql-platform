namespace HotChocolate.Fusion.Options;

/// <summary>
/// Defines how to handle tag directives when merging source schemas.
/// </summary>
public enum TagMergeBehavior
{
    /// <summary>
    /// Ignore tag directives.
    /// </summary>
    Ignore = 0,
    /// <summary>
    /// Merge tag directives publicly (<c>@tag</c>).
    /// </summary>
    Include = 1,
    /// <summary>
    /// Merge tag directives privately (<c>@fusion__tag</c>).
    /// </summary>
    IncludePrivate = 2
}
