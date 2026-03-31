namespace Mocha.Sagas.Tests;

/// <summary>
/// Minimal IMessageBus implementation that records operations in a TestMessageOutbox.
/// </summary>
public sealed class TestMessageBus(TestMessageOutbox outbox) : IMessageBus
{
    private int _scheduleCounter;

    public List<string> CancelledTokens { get; } = [];

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

    public ValueTask<SchedulingResult> SchedulePublishAsync<T>(
        T message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken)
        where T : notnull
    {
        var token = $"test:{Interlocked.Increment(ref _scheduleCounter)}";
        outbox.Messages.Add(new TestMessageOutbox.Operation(TestMessageOutbox.OperationKind.Publish, message, null));
        return ValueTask.FromResult(
            new SchedulingResult
            {
                Token = token,
                ScheduledTime = scheduledTime,
                IsCancellable = true
            });
    }

    public ValueTask<SchedulingResult> SchedulePublishAsync<T>(
        T message,
        DateTimeOffset scheduledTime,
        PublishOptions options,
        CancellationToken cancellationToken)
        where T : notnull
    {
        var token = $"test:{Interlocked.Increment(ref _scheduleCounter)}";
        outbox.Messages.Add(new TestMessageOutbox.Operation(TestMessageOutbox.OperationKind.Publish, message, options));
        return ValueTask.FromResult(
            new SchedulingResult
            {
                Token = token,
                ScheduledTime = scheduledTime,
                IsCancellable = true
            });
    }

    public ValueTask<SchedulingResult> ScheduleSendAsync(
        object message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken)
    {
        var token = $"test:{Interlocked.Increment(ref _scheduleCounter)}";
        outbox.Messages.Add(new TestMessageOutbox.Operation(TestMessageOutbox.OperationKind.Send, message, null));
        return ValueTask.FromResult(
            new SchedulingResult
            {
                Token = token,
                ScheduledTime = scheduledTime,
                IsCancellable = true
            });
    }

    public ValueTask<SchedulingResult> ScheduleSendAsync(
        object message,
        DateTimeOffset scheduledTime,
        SendOptions options,
        CancellationToken cancellationToken)
    {
        var token = $"test:{Interlocked.Increment(ref _scheduleCounter)}";
        outbox.Messages.Add(new TestMessageOutbox.Operation(TestMessageOutbox.OperationKind.Send, message, options));
        return ValueTask.FromResult(
            new SchedulingResult
            {
                Token = token,
                ScheduledTime = scheduledTime,
                IsCancellable = true
            });
    }

    public ValueTask<bool> CancelScheduledMessageAsync(string token, CancellationToken cancellationToken)
    {
        CancelledTokens.Add(token);
        return ValueTask.FromResult(true);
    }
}
