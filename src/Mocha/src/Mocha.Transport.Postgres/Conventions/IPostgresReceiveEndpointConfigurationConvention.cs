namespace Mocha.Transport.Postgres;

/// <summary>
/// Convention that applies default configuration values to PostgreSQL receive endpoint configurations.
/// </summary>
public interface IPostgresReceiveEndpointConfigurationConvention
    : IEndpointConfigurationConvention<ReceiveEndpointConfiguration>
{
    void IEndpointConfigurationConvention<ReceiveEndpointConfiguration>.Configure(
        IMessagingConfigurationContext context,
        MessagingTransport transport,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not PostgresReceiveEndpointConfiguration postgresConfiguration)
        {
            return;
        }

        if (transport is not PostgresMessagingTransport postgresTransport)
        {
            return;
        }

        Configure(context, postgresTransport, postgresConfiguration);
    }

    /// <summary>
    /// Applies convention-defined defaults to a PostgreSQL receive endpoint configuration.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="transport">The PostgreSQL messaging transport instance.</param>
    /// <param name="configuration">The strongly-typed PostgreSQL receive endpoint configuration to modify.</param>
    void Configure(
        IMessagingConfigurationContext context,
        PostgresMessagingTransport transport,
        PostgresReceiveEndpointConfiguration configuration);
}
