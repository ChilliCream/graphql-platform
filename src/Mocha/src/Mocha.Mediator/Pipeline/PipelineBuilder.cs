using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;

namespace Mocha.Mediator;

/// <summary>
/// Provides terminal delegate factories for each handler kind.
/// These delegates form the innermost layer of the middleware pipeline,
/// resolving handlers from the scoped service provider and invoking them.
/// This class is intended for use by source-generated code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PipelineBuilder
{
    /// <summary>
    /// Builds a terminal delegate for a void command handler.
    /// </summary>
    public static MediatorDelegate BuildVoidCommandTerminal<TCommand>()
        where TCommand : ICommand
    {
        var serviceType = typeof(ICommandHandler<TCommand>);

        return ctx =>
        {
            var handler = (ICommandHandler<TCommand>)ctx.Services.GetRequiredService(serviceType);
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
    /// Builds a terminal delegate for a command handler that returns a response.
    /// </summary>
    public static MediatorDelegate BuildCommandTerminal<TCommand, TResponse>()
        where TCommand : ICommand<TResponse>
    {
        var serviceType = typeof(ICommandHandler<TCommand, TResponse>);

        return ctx =>
        {
            var handler = (ICommandHandler<TCommand, TResponse>)ctx.Services.GetRequiredService(serviceType);
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
    /// Builds a terminal delegate for a query handler.
    /// </summary>
    public static MediatorDelegate BuildQueryTerminal<TQuery, TResponse>()
        where TQuery : IQuery<TResponse>
    {
        var serviceType = typeof(IQueryHandler<TQuery, TResponse>);

        return ctx =>
        {
            var handler = (IQueryHandler<TQuery, TResponse>)ctx.Services.GetRequiredService(serviceType);
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
    /// Builds a terminal delegate for a notification with handler types known at compile time.
    /// </summary>
    public static MediatorDelegate BuildNotificationTerminal<TNotification>(Type[] handlerTypes)
        where TNotification : INotification
    {
        if (handlerTypes.Length == 1)
        {
            var handlerType = handlerTypes[0];
            return ctx =>
            {
                var handler = (INotificationHandler<TNotification>)ctx.Services.GetRequiredService(handlerType);
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

        return ctx =>
        {
            var strategy = ctx.Runtime.Features
                .GetRequired<NotificationStrategyFeature>().Strategy;

            var handlers = new INotificationHandler<TNotification>[handlerTypes.Length];
            for (var i = 0; i < handlerTypes.Length; i++)
            {
                handlers[i] = (INotificationHandler<TNotification>)ctx.Services.GetRequiredService(handlerTypes[i]);
            }

            var task = strategy.PublishAsync(handlers, (TNotification)ctx.Message, ctx.CancellationToken);

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
