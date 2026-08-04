namespace Mocha.Sagas.EfCore;

/// <summary>
/// Configuration options for the Postgres saga store, containing pre-built SQL queries
/// derived from the saga state table metadata.
/// </summary>
internal sealed class PostgresSagaStoreOptions
{
    /// <summary>
    /// Gets or sets the pre-built SQL queries used for saga state select, insert, update, and delete operations.
    /// </summary>
    public PostgresSagaStoreQueries Queries { get; set; } = null!;
}
