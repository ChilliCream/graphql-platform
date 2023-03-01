namespace HotChocolate.Fusion.Composition.Pipeline;

public sealed class MergePipelineBuilder
{
    private readonly List<MergeMiddleware> _pipeline = new();

    private MergePipelineBuilder()
    {
    }

    public static MergePipelineBuilder New() => new();

    public MergePipelineBuilder Use(MergeMiddleware middleware)
    {
        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        _pipeline.Add(middleware);
        return this;
    }

    public MergePipelineBuilder Use<TMiddleware>()
        where TMiddleware : IMergeMiddleware, new()
        => Use(
            next =>
            {
                var middleware = new TMiddleware();
                return context => middleware.InvokeAsync(context, next);
            });

    public MergePipelineBuilder Use<TMiddleware>(Func<TMiddleware> factory)
        where TMiddleware : IMergeMiddleware
        => Use(
            next =>
            {
                var middleware = factory();
                return context => middleware.InvokeAsync(context, next);
            });

    public MergeDelegate Build()
    {
        MergeDelegate next = _ => default;

        for (var i = _pipeline.Count - 1; i >= 0; i--)
        {
            next = _pipeline[i].Invoke(next);
        }

        return next;
    }
}

public delegate ValueTask MergeDelegate(CompositionContext context);

public delegate MergeDelegate MergeMiddleware(MergeDelegate next);
