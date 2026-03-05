using Mocha;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Minimal IMessageBus implementation that records operations in a TestMessageOutbox.
/// </summary>
public sealed class TestMessageBus(TestMessageOutbox outbox) : IMessageBus
{
    public ValueTask PublishAsync<T>(T message, CancellationToken cancellationToken)
    {
        outbox.Messages.Add(new TestMessageOutbox.Operation(TestMessageOutbox.OperationKind.Publish, message!, null));
        return ValueTask.CompletedTask;
    }

    public ValueTask PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken)
    {
        outbox.Messages.Add(
            new TestMessageOutbox.Operation(TestMessageOutbox.OperationKind.Publish, message!, options));
        return ValueTask.CompletedTask;
    }

    public ValueTask SendAsync(object message, CancellationToken cancellationToken)
    {
        outbox.Messages.Add(new TestMessageOutbox.Operation(TestMessageOutbox.OperationKind.Send, message, null));
        return ValueTask.CompletedTask;
    }

    public ValueTask SendAsync(object message, SendOptions options, CancellationToken cancellationToken)
    {
        outbox.Messages.Add(new TestMessageOutbox.Operation(TestMessageOutbox.OperationKind.Send, message, options));
        return ValueTask.CompletedTask;
    }

    public ValueTask<TResponse> RequestAsync<TResponse>(
        IEventRequest<TResponse> message,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("RequestAsync not supported in test message bus");
    }

    public ValueTask<TResponse> RequestAsync<TResponse>(
        IEventRequest<TResponse> message,
        SendOptions options,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("RequestAsync not supported in test message bus");
    }

    public ValueTask RequestAsync(object message, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("RequestAsync not supported in test message bus");
    }

    public ValueTask RequestAsync(object message, SendOptions options, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("RequestAsync not supported in test message bus");
    }

    public ValueTask ReplyAsync<TResponse>(
        TResponse response,
        ReplyOptions options,
        CancellationToken cancellationToken)
        where TResponse : notnull
    {
        outbox.Messages.Add(new TestMessageOutbox.Operation(TestMessageOutbox.OperationKind.Reply, response, options));
        return ValueTask.CompletedTask;
    }
}
