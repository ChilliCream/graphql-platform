namespace Mocha.Transport.Postgres;

/// <summary>
/// Convention that defaults the queue name to the endpoint name when no explicit queue is specified,
/// materializes typed error and skipped satellite queues using verbatim names when provided,
/// and applies bus-level endpoint defaults (batch size, concurrency) from the transport configuration.
/// </summary>
public sealed class PostgresDefaultReceiveEndpointConvention : IPostgresReceiveEndpointConfigurationConvention
{
    /// <summary>
    /// Sets the queue name to the endpoint name if it has not been explicitly configured,
    /// materializes satellite queues from their typed configuration, and applies bus-level endpoint defaults.
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
            MaterializeSatellite(
                context,
                transport,
                configuration.ErrorQueue,
                queueName,
                ReceiveEndpointKind.Error,
                endpoint => configuration.ErrorEndpoint ??= endpoint);

            MaterializeSatellite(
                context,
                transport,
                configuration.SkippedQueue,
                queueName,
                ReceiveEndpointKind.Skipped,
                endpoint => configuration.SkippedEndpoint ??= endpoint);
        }

        if (transport.Topology is PostgresMessagingTopology topology)
        {
            topology.Defaults.Endpoint.ApplyTo(configuration);
        }
    }

    private static void MaterializeSatellite(
        IMessagingConfigurationContext context,
        PostgresMessagingTransport transport,
        PostgresSatelliteConfiguration satellite,
        string queueName,
        ReceiveEndpointKind kind,
        Action<Uri> assign)
    {
        if (satellite.IsDisabled)
        {
            return;
        }

        // A satellite name is stored verbatim when provided and bypasses the naming convention,
        // so literal names such as "LEGACY.Orders.Error" survive unchanged. When no name is set,
        // the convention-derived name keeps the historical default.
        var name = satellite.QueueName ?? context.Naming.GetReceiveEndpointName(queueName, kind);

        assign(new Uri($"{transport.Schema}:q/{name}"));
    }
}
