using System.Runtime.CompilerServices;

namespace Mocha.Mediator;

/// <summary>
/// Framework-provided mediator implementation that dispatches commands, queries,
/// and notifications through pre-compiled middleware pipelines using O(1) type lookups.
/// </summary>
public sealed class Mediator(MediatorRuntime runtime, IServiceProvider serviceProvider) : IMediator
{
    /// <inheritdoc />
    public ValueTask SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var messageType = command.GetType();
        var pipeline = runtime.GetPipeline(messageType);
        var context = runtime.RentContext();

        context.Initialize(runtime, serviceProvider, command, messageType, cancellationToken);

        var task = pipeline(context);

        if (task.IsCompletedSuccessfully)
        {
            runtime.ReturnContext(context);
            return default;
        }

        return AwaitAndReturn(task, context);
    }

    /// <inheritdoc />
    public ValueTask<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var messageType = command.GetType();
        var pipeline = runtime.GetPipeline(messageType);
        var context = runtime.RentContext();

        context.Initialize(runtime, serviceProvider, command, messageType, cancellationToken, typeof(TResponse));

        var task = pipeline(context);

        if (task.IsCompletedSuccessfully)
        {
            var result = (TResponse)context.Result!;

            runtime.ReturnContext(context);

            return new ValueTask<TResponse>(result);
        }

        return AwaitAndReturn<TResponse>(task, context);
    }

    /// <inheritdoc />
    public ValueTask<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var messageType = query.GetType();
        var pipeline = runtime.GetPipeline(messageType);
        var context = runtime.RentContext();

        context.Initialize(runtime, serviceProvider, query, messageType, cancellationToken, typeof(TResponse));

        var task = pipeline(context);

        if (task.IsCompletedSuccessfully)
        {
            var result = (TResponse)context.Result!;

            runtime.ReturnContext(context);

            return new ValueTask<TResponse>(result);
        }

        return AwaitAndReturn<TResponse>(task, context);
    }

    /// <inheritdoc />
    ValueTask<object?> ISender.SendAsync(object message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = message.GetType();

        var pipeline = runtime.GetPipeline(messageType);

        var context = runtime.RentContext();

        context.Initialize(runtime, serviceProvider, message, messageType, cancellationToken, typeof(object));

        var task = pipeline(context);
        if (task.IsCompletedSuccessfully)
        {
            var result = context.Result;

            runtime.ReturnContext(context);

            return new ValueTask<object?>(result);
        }

        return AwaitAndReturnObject(task, context);
    }

    /// <inheritdoc />
    public ValueTask PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        return PublishCoreAsync(notification, notification.GetType(), cancellationToken);
    }

    /// <inheritdoc />
    ValueTask IPublisher.PublishAsync(object notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return PublishCoreAsync(notification, notification.GetType(), cancellationToken);
    }

    private ValueTask PublishCoreAsync(
        object notification,
        Type messageType,
        CancellationToken cancellationToken)
    {
        var pipelines = runtime.GetNotificationPipelines(messageType);

        if (pipelines.Length == 1)
        {
            return PublishSingle(pipelines[0], notification, messageType, cancellationToken);
        }

        return runtime.NotificationPublishMode == NotificationPublishMode.Concurrent
            ? PublishConcurrently(pipelines, notification, messageType, cancellationToken)
            : PublishSequentially(pipelines, notification, messageType, cancellationToken);
    }

    private ValueTask PublishSingle(
        MediatorDelegate pipeline,
        object notification,
        Type messageType,
        CancellationToken cancellationToken)
    {
        var context = runtime.RentContext();
        context.Initialize(runtime, serviceProvider, notification, messageType, cancellationToken);

        var task = pipeline(context);

        if (task.IsCompletedSuccessfully)
        {
            runtime.ReturnContext(context);
            return default;
        }

        return AwaitAndReturn(task, context);
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    private async ValueTask PublishSequentially(
        MediatorDelegate[] pipelines,
        object notification,
        Type messageType,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < pipelines.Length; i++)
        {
            var context = runtime.RentContext();
            try
            {
                context.Initialize(runtime, serviceProvider, notification, messageType, cancellationToken);
                await pipelines[i](context).ConfigureAwait(false);
            }
            finally
            {
                runtime.ReturnContext(context);
            }
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    private async ValueTask PublishConcurrently(
        MediatorDelegate[] pipelines,
        object notification,
        Type messageType,
        CancellationToken cancellationToken)
    {
        var count = pipelines.Length;
        var contexts = new MediatorContext[count];
        var tasks = new Task[count];

        for (var i = 0; i < count; i++)
        {
            var context = runtime.RentContext();
            context.Initialize(runtime, serviceProvider, notification, messageType, cancellationToken);
            contexts[i] = context;
            tasks[i] = pipelines[i](context).AsTask();
        }

        try
        {
            var whenAll = Task.WhenAll(tasks);

            try
            {
                await whenAll.ConfigureAwait(false);
            }
            catch
            {
                // Task.WhenAll captures all exceptions, but await unwraps only the first.
                // Re-throw the AggregateException to surface all failures.
                if (whenAll.Exception is not null)
                {
                    throw whenAll.Exception;
                }

                throw;
            }
        }
        finally
        {
            for (var i = 0; i < count; i++)
            {
                runtime.ReturnContext(contexts[i]);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    private async ValueTask AwaitAndReturn(ValueTask task, MediatorContext context)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        finally
        {
            runtime.ReturnContext(context);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<TResponse> AwaitAndReturn<TResponse>(ValueTask task, MediatorContext context)
    {
        try
        {
            await task.ConfigureAwait(false);

            return (TResponse)context.Result!;
        }
        finally
        {
            runtime.ReturnContext(context);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<object?> AwaitAndReturnObject(ValueTask task, MediatorContext context)
    {
        try
        {
            await task.ConfigureAwait(false);

            return context.Result;
        }
        finally
        {
            runtime.ReturnContext(context);
        }
    }
}
