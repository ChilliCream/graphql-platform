using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.ElasticSearch;

namespace HotChocolate.Data;

/// <summary>
/// Common extensions of <see cref="IQueryable{T}"/> for Elasticsearch.
/// </summary>
[Experimental(ElasticSearchExperiments.Provider)]
public static class ElasticSearchExecutableExtensions
{
    /// <summary>
    /// Wraps an ES|QL queryable with an executable that runs it asynchronously.
    /// </summary>
    /// <param name="source">The ES|QL queryable.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>The executable.</returns>
    public static IExecutable<T> AsElasticSearchExecutable<T>(this IQueryable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new ElasticSearchExecutable<T>(source);
    }
}
