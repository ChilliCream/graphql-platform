namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// A builder class for constructing a merge pipeline.
/// </summary>
internal sealed class MergePipelineBuilder
{
    private readonly List<MergeMiddleware> _pipeline = [];

    private MergePipelineBuilder()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="MergePipelineBuilder"/> class.
    /// </summary>
    public static MergePipelineBuilder New() => new();

    /// <summary>
    /// Adds a middleware to the end of the pipeline.
    /// </summary>
    /// <param name="middleware">The middleware to add.</param>
    public MergePipelineBuilder Use(MergeMiddleware middleware)
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
    public MergePipelineBuilder Use<TMiddleware>()
        where TMiddleware : IMergeMiddleware, new()
        => Use(
            next =>
            {
                var middleware = new TMiddleware();
                return context => middleware.InvokeAsync(context, next);
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
    public MergePipelineBuilder Use<TMiddleware>(Func<TMiddleware> factory)
        where TMiddleware : IMergeMiddleware
        => Use(
            next =>
            {
                var middleware = factory();
                return context => middleware.InvokeAsync(context, next);
            });

    /// <summary>
    /// Builds the merge pipeline.
    /// </summary>
    /// <returns>
    /// A delegate that represents the merge pipeline.
    /// </returns>
    public MergeDelegate Build()
    {
        // Start with a default delegate that does nothing.
        MergeDelegate next = _ => default;

        // Apply the middleware in reverse order.
        for (var i = _pipeline.Count - 1; i >= 0; i--)
        {
            next = _pipeline[i].Invoke(next);
        }

        return next;
    }
}

/// <summary>
/// A delegate that represents a middleware in the merge pipeline.
/// </summary>
internal delegate MergeDelegate MergeMiddleware(MergeDelegate next);

/// <summary>
/// A delegate that represents a step in the merge pipeline.
/// </summary>
internal delegate ValueTask MergeDelegate(CompositionContext context);
