namespace Mocha.Mediator;

/// <summary>
/// Compiles ordered middleware configurations into a single executable <see cref="MediatorDelegate"/>.
/// Mirrors the message bus <c>MiddlewareCompiler</c> pattern.
/// </summary>
internal static class MediatorMiddlewareCompiler
{
    private static List<MediatorMiddlewareConfiguration>? s_middlewares;

    public static MediatorDelegate Compile(
        MediatorMiddlewareFactoryContext context,
        MediatorDelegate terminal,
        ReadOnlySpan<IReadOnlyList<MediatorMiddlewareConfiguration>> middlewareConfigurations,
        ReadOnlySpan<IReadOnlyList<Action<List<MediatorMiddlewareConfiguration>>>> pipelineModifiers)
    {
        var middlewares = Interlocked.Exchange(ref s_middlewares, null);
        middlewares ??= [];

        foreach (var middleware in middlewareConfigurations)
        {
            middlewares.AddRange(middleware);
        }

        foreach (var modifiers in pipelineModifiers)
        {
            foreach (var modifier in modifiers)
            {
                modifier(middlewares);
            }
        }

        middlewares.Reverse();

        var pipeline = terminal;

        foreach (var middleware in middlewares)
        {
            var next = pipeline;
            pipeline = middleware.Middleware(context, next);
        }

        middlewares.Clear();

        Interlocked.CompareExchange(ref s_middlewares, middlewares, null);

        return pipeline;
    }
}
