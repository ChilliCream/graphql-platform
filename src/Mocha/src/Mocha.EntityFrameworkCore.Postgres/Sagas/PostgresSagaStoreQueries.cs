using Mocha.EntityFrameworkCore.Postgres;

namespace Mocha.Sagas.EfCore;

/// <summary>
/// Holds pre-built SQL query strings for Postgres saga state operations, generated from
/// <see cref="SagaStateTableInfo"/> column and table metadata.
/// </summary>
internal sealed class PostgresSagaStoreQueries
{
    /// <summary>
    /// Gets or sets the SQL query to select the serialized saga state by id and saga name.
    /// </summary>
    public string SelectState { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL query to select the concurrency version by id and saga name.
    /// </summary>
    public string SelectVersion { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement to insert a new saga state record.
    /// </summary>
    public string InsertState { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement to update an existing saga state record with optimistic concurrency.
    /// </summary>
    public string UpdateState { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement to delete a saga state record by id and saga name.
    /// </summary>
    public string DeleteState { get; set; } = null!;

    /// <summary>
    /// Creates a new <see cref="PostgresSagaStoreQueries"/> instance with SQL queries built from the provided table metadata.
    /// </summary>
    /// <param name="t">The saga state table info containing column and table names.</param>
    /// <returns>A fully initialized <see cref="PostgresSagaStoreQueries"/> instance.</returns>
    public static PostgresSagaStoreQueries From(SagaStateTableInfo t)
    {
        return new PostgresSagaStoreQueries
        {
            SelectState = $"""
                SELECT "{t.State}"
                FROM {t.QualifiedTableName}
                WHERE "{t.Id}" = @id AND "{t.SagaName}" = @sagaName;
                """,

            SelectVersion = $"""
                SELECT "{t.Version}"
                FROM {t.QualifiedTableName}
                WHERE "{t.Id}" = @id AND "{t.SagaName}" = @sagaName;
                """,

            InsertState = $"""
                INSERT INTO {t.QualifiedTableName}
                    ("{t.Id}", "{t.SagaName}", "{t.State}", "{t.CreatedAt}", "{t.UpdatedAt}", "{t.Version}")
                VALUES
                    (@id, @sagaName, @state, @createdAt, @updatedAt, @version);
                """,

            UpdateState = $"""
                UPDATE {t.QualifiedTableName}
                SET "{t.State}" = @state,
                    "{t.UpdatedAt}" = @updatedAt,
                    "{t.Version}" = @newVersion
                WHERE "{t.Id}" = @id
                  AND "{t.SagaName}" = @sagaName
                  AND "{t.Version}" = @oldVersion;
                """,

            DeleteState = $"""
                DELETE FROM {t.QualifiedTableName}
                WHERE "{t.Id}" = @id AND "{t.SagaName}" = @sagaName;
                """
        };
    }
}
