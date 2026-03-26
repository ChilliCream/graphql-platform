using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;

namespace Mocha.Mediator;

/// <summary>
/// Provides pipeline delegate factories for each handler kind.
/// These delegates form the innermost layer of the middleware pipeline,
/// resolving handlers from the scoped service provider and invoking them.
/// This class is intended for use by source-generated code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PipelineBuilder
{
    /// <summary>
    /// Builds a pipeline delegate for a void command handler.
    /// </summary>
    public static MediatorDelegate BuildCommandPipeline<THandler, TCommand>()
        where THandler : class, ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        return static ctx =>
        {
            var handler = ctx.Services.GetRequiredService<THandler>();
            var task = handler.HandleAsync((TCommand)ctx.Message, ctx.CancellationToken);

            if (task.IsCompletedSuccessfully)
            {
                return default;
            }

            return Awaited(task);

            [MethodImpl(MethodImplOptions.NoInlining)]
            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            static async ValueTask Awaited(ValueTask t)
            {
                await t.ConfigureAwait(false);
            }
        };
    }

    /// <summary>
    /// Builds a pipeline delegate for a command handler that returns a response.
    /// </summary>
    public static MediatorDelegate BuildCommandResponsePipeline<THandler, TCommand, TResponse>()
        where THandler : class, ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        return static ctx =>
        {
            var handler = ctx.Services.GetRequiredService<THandler>();
            var task = handler.HandleAsync((TCommand)ctx.Message, ctx.CancellationToken);

            if (task.IsCompletedSuccessfully)
            {
                ctx.Result = task.Result;
                return default;
            }

            return Awaited(task, ctx);

            [MethodImpl(MethodImplOptions.NoInlining)]
            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            static async ValueTask Awaited(ValueTask<TResponse> t, IMediatorContext c)
            {
                c.Result = await t.ConfigureAwait(false);
            }
        };
    }

    /// <summary>
    /// Builds a pipeline delegate for a query handler.
    /// </summary>
    public static MediatorDelegate BuildQueryPipeline<THandler, TQuery, TResponse>()
        where THandler : class, IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        return static ctx =>
        {
            var handler = ctx.Services.GetRequiredService<THandler>();
            var task = handler.HandleAsync((TQuery)ctx.Message, ctx.CancellationToken);

            if (task.IsCompletedSuccessfully)
            {
                ctx.Result = task.Result;
                return default;
            }

            return Awaited(task, ctx);

            [MethodImpl(MethodImplOptions.NoInlining)]
            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            static async ValueTask Awaited(ValueTask<TResponse> t, IMediatorContext c)
            {
                c.Result = await t.ConfigureAwait(false);
            }
        };
    }

    /// <summary>
    /// Builds a pipeline delegate for a single notification handler.
    /// </summary>
    public static MediatorDelegate BuildNotificationPipeline<THandler, TNotification>()
        where THandler : class, INotificationHandler<TNotification>
        where TNotification : INotification
    {
        return static ctx =>
        {
            var handler = ctx.Services.GetRequiredService<THandler>();
            var task = handler.HandleAsync((TNotification)ctx.Message, ctx.CancellationToken);

            if (task.IsCompletedSuccessfully)
            {
                return default;
            }

            return Awaited(task);

            [MethodImpl(MethodImplOptions.NoInlining)]
            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            static async ValueTask Awaited(ValueTask t)
            {
                await t.ConfigureAwait(false);
            }
        };
    }
}
