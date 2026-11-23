namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Reports a started RabbitMQ transport as unhealthy while its consumer connection is not ready or one of its
/// consumers is not attached.
/// </summary>
/// <param name="transport">The transport to check.</param>
internal sealed class RabbitMQConsumerHealthCheck(RabbitMQMessagingTransport transport) : IMessageBusHealthCheck
{
    /// <inheritdoc />
    public ValueTask CheckAsync(MessageBusHealthContext context, CancellationToken cancellationToken)
    {
        if (!transport.IsStarted)
        {
            return ValueTask.CompletedTask;
        }

        var manager = transport.ConsumerManager;
        if (!manager.IsConnected)
        {
            context.ReportUnhealthy(transport.Name, "The consumer connection is unavailable.");
            return ValueTask.CompletedTask;
        }

        foreach (var consumer in manager.Consumers)
        {
            if (consumer.IsAttached)
            {
                continue;
            }

            var reason = consumer.State switch
            {
                ConsumerState.WaitingForHandlers => "The consumer waits for a handler of a lost channel to return.",
                ConsumerState.Stopped => "The consumer is stopped.",
                _ => "The consumer is not attached."
            };
            context.ReportUnhealthy(transport.Name, $"Queue '{consumer.QueueName}': {reason}");
            break;
        }

        return ValueTask.CompletedTask;
    }
}
