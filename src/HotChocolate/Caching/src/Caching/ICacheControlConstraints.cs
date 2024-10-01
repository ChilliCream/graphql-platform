using System.Collections.Immutable;
using HotChocolate.Execution;

namespace HotChocolate.Caching;

/// <summary>
/// The constraints that apply to the caching
/// of a query result.
/// </summary>
public interface ICacheConstraints
{
    /// <summary>
    /// The maximum time the query result shall be cached,
    /// in seconds.
    /// </summary>
    int? MaxAge { get; }
    /// <summary>
    /// The maximum time the query result shall be cached in a shared cache,
    /// in seconds.
    /// </summary>
    int? SharedMaxAge { get; }

    /// <summary>
    /// The scope of the <see cref="IOperationResult"/> that shall be cached.
    /// </summary>
    CacheControlScope Scope { get; }

    /// <summary>
    /// Headers that shall be used to determine the cache key.
    /// </summary>
    ImmutableArray<string> Vary { get; }
}
