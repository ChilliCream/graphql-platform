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

        var messageType = notification.GetType();
        var pipeline = runtime.GetPipeline(messageType);
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

    /// <inheritdoc />
    ValueTask IPublisher.PublishAsync(object notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var messageType = notification.GetType();
        var pipeline = runtime.GetPipeline(messageType);
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
