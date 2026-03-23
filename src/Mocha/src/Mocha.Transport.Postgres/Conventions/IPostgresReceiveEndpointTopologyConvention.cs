namespace Mocha.Transport.Postgres;

/// <summary>
/// Convention that discovers and provisions topology resources for PostgreSQL receive endpoints.
/// </summary>
public interface IPostgresReceiveEndpointTopologyConvention
    : IReceiveEndpointTopologyConvention<PostgresReceiveEndpoint, PostgresReceiveEndpointConfiguration>;
