namespace HotChocolate.Fusion.Planning;

/// <summary>
/// This interface represents a middleware of the query planing pipeline.
/// </summary>
internal interface IQueryPlanMiddleware
{
    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">
    /// The query plan context.
    /// </param>
    /// <param name="next">\
    /// The next middleware in the pipeline.
    /// </param>
    void Invoke(
        QueryPlanContext context,
        QueryPlanDelegate next);
}

/// <summary>
/// A delegate that represents a middleware to build the query plan.
/// </summary>
internal delegate QueryPlanDelegate QueryPlanMiddleware(QueryPlanDelegate next);

/// <summary>
/// A delegate that represents one component of the query planing pipeline.
/// </summary>
internal delegate void QueryPlanDelegate(QueryPlanContext context);
