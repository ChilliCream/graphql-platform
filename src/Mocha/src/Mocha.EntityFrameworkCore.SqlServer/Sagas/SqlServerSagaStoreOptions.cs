namespace Mocha.Sagas.EfCore;

/// <summary>
/// Configuration options for the SQL Server saga store, containing pre-built SQL queries
/// derived from the saga state table metadata.
/// </summary>
internal sealed class SqlServerSagaStoreOptions
{
    /// <summary>
    /// Gets or sets the pre-built SQL queries used for saga state select, insert, update, and delete operations.
    /// </summary>
    public SqlServerSagaStoreQueries Queries { get; set; } = null!;
}
