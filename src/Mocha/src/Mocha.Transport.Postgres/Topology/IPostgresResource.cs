namespace Mocha.Transport.Postgres;

/// <summary>
/// Represents a PostgreSQL topology resource that can be provisioned in the database.
/// </summary>
public interface IPostgresResource
{
    /// <summary>
    /// Provisions this resource in the PostgreSQL database.
    /// </summary>
    Task ProvisionAsync(
        PostgresConnectionManager connectionManager,
        IReadOnlyPostgresSchemaOptions schemaOptions,
        CancellationToken cancellationToken);
}
