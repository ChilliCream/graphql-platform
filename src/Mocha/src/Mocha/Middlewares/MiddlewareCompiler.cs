using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Compiles ordered middleware configurations into executable delegates for dispatch, receive, and
/// consume pipelines.
/// </summary>
/// <remarks>
/// Middleware lists are materialized, modified, reversed, and then folded right-to-left so
/// registration order remains intuitive while execution follows the standard nested middleware
/// pattern.
/// Without this compile step, each receive/dispatch would need repeated per-message composition and
/// ordering could drift when modifiers are applied.
/// </remarks>
internal static class MiddlewareCompiler
{
    private static List<DispatchMiddlewareConfiguration>? s_dispatchMiddlewares;
    private static List<ReceiveMiddlewareConfiguration>? s_receiveMiddlewares;
    private static List<ConsumerMiddlewareConfiguration>? s_handlerMiddlewares;

    public static DispatchDelegate CompileDispatch(
        DispatchMiddlewareFactoryContext context,
        DispatchDelegate dispatch,
        ReadOnlySpan<IReadOnlyList<DispatchMiddlewareConfiguration>> middlewareConfigurations,
        ReadOnlySpan<IReadOnlyList<Action<List<DispatchMiddlewareConfiguration>>>> pipelineModifiers)
    {
        // Atomically claim the reusable list instance, if one is currently cached.
        var middlewares = Interlocked.Exchange(ref s_dispatchMiddlewares, null);
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

        // Reverse before fold so first configured middleware becomes outermost in execution.
        middlewares.Reverse();

        var pipeline = dispatch;

        foreach (var middleware in middlewares)
        {
            var next = pipeline;
            pipeline = middleware.Middleware(context, next);
        }

        middlewares.Clear();

        Interlocked.CompareExchange(ref s_dispatchMiddlewares, middlewares, null);

        return pipeline;
    }

    public static ReceiveDelegate CompileReceive(
        ReceiveMiddlewareFactoryContext context,
        ReceiveDelegate receive,
        ReadOnlySpan<IReadOnlyList<ReceiveMiddlewareConfiguration>> middlewareConfigurations,
        ReadOnlySpan<IReadOnlyList<Action<List<ReceiveMiddlewareConfiguration>>>> pipelineModifiers)
    {
        // Atomically claim the reusable list instance, if one is currently cached.
        var middlewares = Interlocked.Exchange(ref s_receiveMiddlewares, null);
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

        var pipeline = receive;

        foreach (var middleware in middlewares)
        {
            var next = pipeline;
            pipeline = middleware.Middleware(context, next);
        }

        middlewares.Clear();

        Interlocked.CompareExchange(ref s_receiveMiddlewares, middlewares, null);

        return pipeline;
    }

    public static ConsumerDelegate CompileHandler(
        ConsumerMiddlewareFactoryContext context,
        ConsumerDelegate handler,
        ReadOnlySpan<IReadOnlyList<ConsumerMiddlewareConfiguration>> middlewareConfigurations,
        ReadOnlySpan<IReadOnlyList<Action<List<ConsumerMiddlewareConfiguration>>>> pipelineModifiers)
    {
        // Atomically claim the reusable list instance, if one is currently cached.
        var middlewares = Interlocked.Exchange(ref s_handlerMiddlewares, null);
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

        // Reverse before fold so first configured middleware becomes outermost in execution.
        var pipeline = handler;

        middlewares.Reverse();

        foreach (var middleware in middlewares)
        {
            var next = pipeline;
            pipeline = middleware.Middleware(context, next);
        }

        middlewares.Clear();

        Interlocked.CompareExchange(ref s_handlerMiddlewares, middlewares, null);

        return pipeline;
    }
}
