using System;
using System.Collections.Generic;
using System.Linq;

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
    /// <typeparam name="T">
    /// The type of the elements that are returned by the query.
    /// </typeparam>
    /// <returns>
    /// Returns a new executable.
    /// </returns>
    public static IExecutable<T> From<T>(IQueryable<T> source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return new DefaultQueryableExecutable<T>(source);
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
