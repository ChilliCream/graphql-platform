namespace Mocha.Transport.InMemory;

/// <summary>
/// Convention that defaults the queue name to the endpoint name when no explicit queue is specified.
/// </summary>
public sealed class InMemoryDefaultReceiveEndpointEndpointConvention : IInMemoryReceiveEndpointConfigurationConvention
{
    /// <summary>
    /// Sets the queue name to the endpoint name if it has not been explicitly configured.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The receive endpoint configuration to apply defaults to.</param>
    public void Configure(IMessagingConfigurationContext context, InMemoryReceiveEndpointConfiguration configuration)
    {
        configuration.QueueName ??= configuration.Name;
    }
}
