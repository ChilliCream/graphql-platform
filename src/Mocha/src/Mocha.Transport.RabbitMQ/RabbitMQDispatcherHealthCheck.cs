namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Reports a started RabbitMQ transport as unhealthy while its dispatcher connection is not ready or the broker
/// blocks it.
/// </summary>
/// <param name="transport">The transport to check.</param>
internal sealed class RabbitMQDispatcherHealthCheck(RabbitMQMessagingTransport transport) : IMessageBusHealthCheck
{
    /// <inheritdoc />
    public ValueTask CheckAsync(MessageBusHealthContext context, CancellationToken cancellationToken)
    {
        if (!transport.IsStarted)
        {
            return ValueTask.CompletedTask;
        }

        var dispatcher = transport.Dispatcher;
        if (!dispatcher.IsConnected)
        {
            context.ReportUnhealthy(transport.Name, "The dispatcher connection is unavailable.");
        }
        else if (dispatcher.IsBlocked)
        {
            context.ReportUnhealthy(transport.Name, "The broker blocks the dispatcher connection.");
        }

        return ValueTask.CompletedTask;
    }
}
