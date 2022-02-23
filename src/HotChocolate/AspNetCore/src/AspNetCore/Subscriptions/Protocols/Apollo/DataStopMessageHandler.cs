namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal sealed class DataStopMessageHandler : MessageHandler<DataStopMessage>
{
    protected override Task HandleAsync(
        ISocketConnection connection,
        DataStopMessage message,
        CancellationToken cancellationToken)
    {
        connection.Subscriptions.Unregister(message.Id);
        return Task.CompletedTask;
    }
}
