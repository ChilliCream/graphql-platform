#nullable enable

namespace HotChocolate.Resolvers;

/// <summary>
/// Specifies when the cleanup task shall be applied.
/// </summary>
public enum CleanAfter
{
    /// <summary>
    /// The cleanup task shall be applied after the resolver task is completed.
    /// </summary>
    Resolver,

    /// <summary>
    /// The cleanup task shall be applied when the query result is being disposed.
    /// </summary>
    Request
}
