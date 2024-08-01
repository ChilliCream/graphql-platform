using System.Collections;

namespace HotChocolate;

/// <summary>
/// Provides utility methods to create executable data sources.
/// </summary>
public static class Executable
{
    /// <summary>
    /// Creates a new executable from a queryable.
    /// </summary>
    /// <param name="source">
    /// The queryable that represents a not yet executed query.
    /// </param>
    /// <param name="queryPrinter">
    /// A delegate that can be used to print the query.
    /// </param>
    /// <typeparam name="T">
    /// The type of the elements that are returned by the query.
    /// </typeparam>
    /// <returns>
    /// Returns a new executable.
    /// </returns>
    public static IQueryableExecutable<T> From<T>(
        IQueryable<T> source,
        Func<IQueryable<T>, string>? queryPrinter = null)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return new DefaultQueryableExecutable<T>(source, queryPrinter);
    }

    /// <summary>
    /// Creates a new executable from an enumerable.
    /// </summary>
    /// <param name="source">
    /// The enumerable.
    /// </param>
    /// <returns>
    /// Returns a new executable.
    /// </returns>
    public static IExecutable From(IEnumerable source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return new DefaultEnumerableExecutable(source);
    }

    /// <summary>
    /// Creates a new executable from an async enumerable.
    /// </summary>
    /// <param name="source">
    /// The async enumerable that is not yet executed.
    /// </param>
    /// <typeparam name="T">
    /// The type of the elements that are returned by the async enumerable.
    /// </typeparam>
    /// <returns>
    /// Returns a new executable.
    /// </returns>
    public static IExecutable<T> From<T>(IAsyncEnumerable<T> source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return new DefaultAsyncEnumerableExecutable<T>(source);
    }
}
