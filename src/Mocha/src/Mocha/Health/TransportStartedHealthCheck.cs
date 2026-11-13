namespace Mocha;

/// <summary>
/// Reports a transport as unhealthy while the transport or one of its receive endpoints is not started.
/// </summary>
/// <param name="transport">The transport to check.</param>
public sealed class TransportStartedHealthCheck(MessagingTransport transport) : IMessageBusHealthCheck
{
    /// <inheritdoc />
    public ValueTask CheckAsync(MessageBusHealthContext context, CancellationToken cancellationToken)
    {
        if (!transport.IsStarted)
        {
            context.ReportUnhealthy(transport.Name, $"Transport '{transport.Name}' is not started.");
            return ValueTask.CompletedTask;
        }

        foreach (var endpoint in transport.ReceiveEndpoints)
        {
            if (!endpoint.IsStarted)
            {
                context.ReportUnhealthy(
                    transport.Name,
                    $"Receive endpoint '{endpoint.Name}' of transport '{transport.Name}' is not started.");
                break;
            }
        }

        return ValueTask.CompletedTask;
    }
}
