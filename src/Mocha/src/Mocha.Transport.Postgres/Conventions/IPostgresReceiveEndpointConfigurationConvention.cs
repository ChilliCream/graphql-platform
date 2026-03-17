namespace Mocha.Transport.Postgres;

/// <summary>
/// Convention that applies default configuration values to PostgreSQL receive endpoint configurations.
/// </summary>
public interface IPostgresReceiveEndpointConfigurationConvention : IReceiveEndpointConvention
{
    void IConfigurationConvention<ReceiveEndpointConfiguration>.Configure(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not PostgresReceiveEndpointConfiguration postgresConfiguration)
        {
            return;
        }

        Configure(context, postgresConfiguration);
    }

    /// <summary>
    /// Applies convention-defined defaults to a PostgreSQL receive endpoint configuration.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The strongly-typed PostgreSQL receive endpoint configuration to modify.</param>
    void Configure(IMessagingConfigurationContext context, PostgresReceiveEndpointConfiguration configuration);
}
