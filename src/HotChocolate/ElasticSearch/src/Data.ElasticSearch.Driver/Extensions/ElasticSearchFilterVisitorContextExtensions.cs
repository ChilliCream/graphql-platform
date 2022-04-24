using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data.ElasticSearch;

/// <summary>
/// Provides extensions for <see cref="ElasticSearchFilterVisitorContext"/>
/// </summary>
public static class ElasticSearchFilterVisitorContextExtensions
{
    /// <summary>
    /// Reads the current scope from the context
    /// </summary>
    /// <param name="context">The context</param>
    /// <returns>The current scope</returns>
    public static ElasticSearchFilterScope GetElasticSearchFilterScope(
        this ElasticSearchFilterVisitorContext context) =>
        (ElasticSearchFilterScope)context.GetScope();

    /// <summary>
    /// Returns the currently selected path of this context
    /// </summary>
    public static string GetPath(this ElasticSearchFilterVisitorContext context) =>
        string.Join(".", context.Path.Reverse());


    /// <summary>
    /// Tries to build the query based on the items that are stored on the scope
    /// </summary>
    /// <param name="context">the context</param>
    /// <param name="query">The query that was build</param>
    /// <returns>True in case the query has been build successfully, otherwise false</returns>
    public static bool TryCreateQuery(
        this ElasticSearchFilterVisitorContext context,
        [NotNullWhen(true)] out BoolOperation? query)
    {
        return context.GetElasticSearchFilterScope().TryCreateQuery(out query);
    }
}
