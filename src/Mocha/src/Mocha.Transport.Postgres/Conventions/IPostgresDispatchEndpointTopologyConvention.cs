namespace Mocha.Transport.Postgres;

/// <summary>
/// Convention that discovers and provisions topology resources for PostgreSQL dispatch endpoints.
/// </summary>
public interface IPostgresDispatchEndpointTopologyConvention
    : IDispatchEndpointTopologyConvention<PostgresDispatchEndpoint, PostgresDispatchEndpointConfiguration>;
