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
    int MaxAge { get; }

    /// <summary>
    /// The scope of the <see cref="IOperationResult"/> that shall be cached.
    /// </summary>
    CacheControlScope Scope { get; }
}
