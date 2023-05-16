using HotChocolate.Resolvers;
using Raven.Client.Documents.Linq;

namespace HotChocolate.Data;

/// <summary>
/// Extensions for sorting, filtering and projection for <see cref="IRavenQueryable{T}"/>
/// </summary>
public static class RavenDataExtensions
{
    /// <summary>
    /// Sorts the selection set of the request onto the queryable.
    /// </summary>
    /// <param name="queryable">The queryable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseSorting
    /// </param>
    /// <returns>The sorted queryable</returns>
    public static IRavenQueryable<T> Sort<T>(
        this IRavenQueryable<T> queryable,
        IResolverContext context) =>
        (IRavenQueryable<T>)QueryableSortExtensions.Sort(queryable, context);

    /// <summary>
    /// Filters the selection set of the request onto the queryable.
    /// </summary>
    /// <param name="queryable">The queryable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseFiltering
    /// </param>
    /// <returns>The sorted queryable</returns>
    public static IRavenQueryable<T> Filter<T>(
        this IRavenQueryable<T> queryable,
        IResolverContext context) =>
        (IRavenQueryable<T>)QueryableFilterExtensions.Filter(queryable, context);

    /// <summary>
    /// Projections the selection set of the request onto the queryable.
    /// </summary>
    /// <param name="queryable">The queryable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseProjection
    /// </param>
    /// <returns>The sorted queryable</returns>
    public static IRavenQueryable<T> Project<T>(
        this IRavenQueryable<T> queryable,
        IResolverContext context) =>
        (IRavenQueryable<T>)QueryableProjectExtensions.Project(queryable, context);
}
