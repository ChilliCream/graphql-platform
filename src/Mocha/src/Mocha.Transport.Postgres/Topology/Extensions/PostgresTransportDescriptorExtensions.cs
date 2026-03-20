using Mocha.Transport.Postgres.Middlewares;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Extension methods for adding default conventions to the PostgreSQL transport descriptor.
/// </summary>
public static class PostgresTransportDescriptorExtensions
{
    /// <summary>
    /// Adds default conventions for receive endpoint configuration, receive endpoint topology,
    /// and dispatch endpoint topology provisioning, plus the parsing middleware.
    /// </summary>
    internal static IPostgresMessagingTransportDescriptor AddDefaults(
        this IPostgresMessagingTransportDescriptor descriptor)
    {
        descriptor.AddConvention(new PostgresDefaultReceiveEndpointConvention());
        descriptor.AddConvention(new PostgresReceiveEndpointTopologyConvention());
        descriptor.AddConvention(new PostgresDispatchEndpointTopologyConvention());

        descriptor.AppendReceive(ReceiveMiddlewares.ConcurrencyLimiter.Key, PostgresReceiveMiddlewares.Parsing);

        return descriptor;
    }
}
