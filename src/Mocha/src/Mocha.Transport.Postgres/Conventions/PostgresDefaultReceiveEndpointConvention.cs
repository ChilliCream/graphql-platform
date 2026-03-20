namespace Mocha.Transport.Postgres;

/// <summary>
/// Convention that defaults the queue name to the endpoint name when no explicit queue is specified,
/// and applies bus-level endpoint defaults (batch size, concurrency) from the transport configuration.
/// </summary>
public sealed class PostgresDefaultReceiveEndpointConvention : IPostgresReceiveEndpointConfigurationConvention
{
    /// <summary>
    /// Sets the queue name to the endpoint name if it has not been explicitly configured,
    /// and applies bus-level endpoint defaults.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="transport">The PostgreSQL messaging transport instance.</param>
    /// <param name="configuration">The receive endpoint configuration to apply defaults to.</param>
    public void Configure(
        IMessagingConfigurationContext context,
        PostgresMessagingTransport transport,
        PostgresReceiveEndpointConfiguration configuration)
    {
        configuration.QueueName ??= configuration.Name;

        if (configuration is { Kind: ReceiveEndpointKind.Default, QueueName: { } queueName })
        {
            if (configuration.ErrorEndpoint is null)
            {
                var errorName = context.Naming.GetReceiveEndpointName(queueName, ReceiveEndpointKind.Error);
                configuration.ErrorEndpoint = new UriBuilder
                {
                    Host = "",
                    Scheme = transport.Schema,
                    Path = "q/" + errorName
                }.Uri;
            }

            if (configuration.SkippedEndpoint is null)
            {
                var skippedName = context.Naming.GetReceiveEndpointName(queueName, ReceiveEndpointKind.Skipped);
                configuration.SkippedEndpoint = new UriBuilder
                {
                    Host = "",
                    Scheme = transport.Schema,
                    Path = "q/" + skippedName
                }.Uri;
            }
        }

        if (transport.Topology is PostgresMessagingTopology topology)
        {
            topology.Defaults.Endpoint.ApplyTo(configuration);
        }
    }
}
