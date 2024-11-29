namespace HotChocolate.Fusion.Planning;

/// <summary>
/// A builder class for constructing a merge pipeline.
/// </summary>
internal sealed class QueryPlanPipelineBuilder
{
    private readonly List<QueryPlanMiddleware> _pipeline = [];

    private QueryPlanPipelineBuilder()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="QueryPlanPipelineBuilder"/> class.
    /// </summary>
    public static QueryPlanPipelineBuilder New() => new();

    /// <summary>
    /// Adds a middleware to the end of the pipeline.
    /// </summary>
    /// <param name="middleware">The middleware to add.</param>
    public QueryPlanPipelineBuilder Use(QueryPlanMiddleware middleware)
    {
        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        _pipeline.Add(middleware);
        return this;
    }

    /// <summary>
    /// Adds a middleware to the end of the pipeline using a default constructor.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware type.</typeparam>
    public QueryPlanPipelineBuilder Use<TMiddleware>()
        where TMiddleware : IQueryPlanMiddleware, new()
        => Use(
            next =>
            {
                var middleware = new TMiddleware();
                return context => middleware.Invoke(context, next);
            });

    /// <summary>
    /// Adds a middleware to the end of the pipeline using a factory method.
    /// </summary>
    /// <typeparam name="TMiddleware">
    /// The middleware type.
    /// </typeparam>
    /// <param name="factory">
    /// A factory method that creates an instance of the middleware.
    /// </param>
    public QueryPlanPipelineBuilder Use<TMiddleware>(Func<TMiddleware> factory)
        where TMiddleware : IQueryPlanMiddleware
        => Use(
            next =>
            {
                var middleware = factory();
                return context => middleware.Invoke(context, next);
            });

    /// <summary>
    /// Builds the plan pipeline.
    /// </summary>
    /// <returns>
    /// A delegate that represents the plan pipeline.
    /// </returns>
    public QueryPlanDelegate Build()
    {
        // Start with a default delegate that does nothing.
        QueryPlanDelegate next = _ => { };

        // Apply the middleware in reverse order.
        for (var i = _pipeline.Count - 1; i >= 0; i--)
        {
            next = _pipeline[i].Invoke(next);
        }

        return next;
    }
}
