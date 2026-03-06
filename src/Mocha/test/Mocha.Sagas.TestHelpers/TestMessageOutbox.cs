using Mocha.Middlewares;
using Mocha.Outbox;

namespace Mocha.Sagas.Tests;

public class TestMessageOutbox : IMessageOutbox
{
    public List<Operation> Messages { get; } = [];

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken) where T : notnull
    {
        Messages.Add(new Operation(OperationKind.Publish, message, null));
        return Task.CompletedTask;
    }

    public void AddPublish<T>(T message, CancellationToken cancellationToken) where T : notnull
    {
        Messages.Add(new Operation(OperationKind.Publish, message, null));
    }

    public Task SendAsync<T>(T message, CancellationToken cancellationToken) where T : notnull
    {
        Messages.Add(new Operation(OperationKind.Send, message, null));
        return Task.CompletedTask;
    }

    public void AddSend<T>(T message, CancellationToken cancellationToken) where T : notnull
    {
        Messages.Add(new Operation(OperationKind.Send, message, null));
    }

    public Task PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken)
        where T : notnull
    {
        Messages.Add(new Operation(OperationKind.Publish, message, options));
        return Task.CompletedTask;
    }

    public void AddPublish<T>(T message, PublishOptions options, CancellationToken cancellationToken) where T : notnull
    {
        Messages.Add(new Operation(OperationKind.Publish, message, options));
    }

    public Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken) where T : notnull
    {
        Messages.Add(new Operation(OperationKind.Send, message, options));
        return Task.CompletedTask;
    }

    public void AddSend<T>(T message, SendOptions options, CancellationToken cancellationToken) where T : notnull
    {
        Messages.Add(new Operation(OperationKind.Send, message, options));
    }

    public Task ReplyAsync<T>(T message, ReplyOptions options, CancellationToken cancellationToken) where T : notnull
    {
        Messages.Add(new Operation(OperationKind.Reply, message, options));
        return Task.CompletedTask;
    }

    public void AddReply<T>(T message, ReplyOptions options, CancellationToken cancellationToken) where T : notnull
    {
        Messages.Add(new Operation(OperationKind.Reply, message, options));
    }

    public Task DispatchAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        // For testing, we don't need to persist envelopes
        return ValueTask.CompletedTask;
    }

    public sealed record Operation(OperationKind Kind, object Message, object? Options);

    public enum OperationKind
    {
        Publish,
        Send,
        Reply
    }
}
