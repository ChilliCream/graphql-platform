namespace HotChocolate;

/// <summary>
/// Represents an executable that has a queryable as its source.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IQueryableExecutable<T> : IExecutable<T>
{
    /// <summary>
    /// The inner queryable.
    /// </summary>
    new IQueryable<T> Source { get; }

    /// <summary>
    /// Defines if the queryable is in memory queryable.
    /// </summary>
    bool IsInMemory { get; }

    /// <summary>
    /// Defines if this executable allows inlining of multiple queries.
    /// </summary>
    bool? AllowsInlining { get; }

    /// <summary>
    /// Returns a new executable with the provided source
    /// </summary>
    /// <param name="source">The source that should be set</param>
    /// <returns>The new instance of an enumerable executable</returns>
    IQueryableExecutable<T> WithSource(IQueryable<T> source);


    /// <summary>
    /// Returns a new executable with the provided source
    /// </summary>
    /// <param name="source">The source that should be set</param>
    /// <returns>The new instance of an enumerable executable</returns>
    IQueryableExecutable<TQuery> WithSource<TQuery>(IQueryable<TQuery> source);
}
