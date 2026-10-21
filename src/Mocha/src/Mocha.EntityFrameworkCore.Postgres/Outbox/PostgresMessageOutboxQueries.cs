using Mocha.EntityFrameworkCore.Postgres;

namespace Mocha.Outbox;

/// <summary>
/// Holds pre-built SQL query strings for Postgres outbox operations, generated from
/// <see cref="OutboxTableInfo"/> column and table metadata.
/// </summary>
internal sealed class PostgresMessageOutboxQueries
{
    /// <summary>
    /// The number of dispatch attempts after which a message is no longer retried.
    /// </summary>
    public const int MaxAttempts = 10;

    /// <summary>
    /// Gets or sets the SQL statement to insert a new message envelope into the outbox table.
    /// </summary>
    public string InsertEnvelope { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL query to compute the next polling interval based on the earliest eligible message.
    /// </summary>
    public string NextPollingInterval { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement that locks a single outbox row for processing,
    /// increments the times-sent counter, and returns the id, envelope and times-sent counter.
    /// </summary>
    public string ProcessEvent { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement to delete a processed outbox message by its identifier.
    /// </summary>
    public string DeleteEvent { get; set; } = null!;

    /// <summary>
    /// Creates a new <see cref="PostgresMessageOutboxQueries"/> instance with SQL queries built from the provided table metadata.
    /// </summary>
    /// <param name="t">The outbox table info containing column and table names.</param>
    /// <returns>A fully initialized <see cref="PostgresMessageOutboxQueries"/> instance.</returns>
    public static PostgresMessageOutboxQueries From(OutboxTableInfo t)
    {
        return new PostgresMessageOutboxQueries
        {
            InsertEnvelope = $"""
                INSERT INTO {t.QualifiedTableName} ("{t.Id}", "{t.Envelope}", "{t.TimesSent}", "{t.CreatedAt}")
                VALUES (@id, @envelope, 0, NOW());
                """,

            NextPollingInterval = $"""
                SELECT MIN(
                    CASE WHEN "{t.TimesSent}" = 0 THEN NOW()
                        ELSE "{t.CreatedAt}"
                    END
                ) AS "NextWakeUpTime"
                FROM {t.QualifiedTableName}
                WHERE "{t.TimesSent}" < {MaxAttempts};
                """,

            ProcessEvent = $"""
                UPDATE {t.QualifiedTableName}
                SET "{t.TimesSent}" = "{t.TimesSent}" + 1,
                    "{t.CreatedAt}" = NOW() + INTERVAL '1 second' * POWER(2, "{t.TimesSent}")
                WHERE "{t.Id}" = (
                    SELECT "{t.Id}" FROM {t.QualifiedTableName}
                    WHERE "{t.TimesSent}" < {MaxAttempts}
                        AND ("{t.TimesSent}" = 0 OR "{t.CreatedAt}" <= NOW())
                    ORDER BY "{t.CreatedAt}"
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                )
                RETURNING
                    "{t.Id}",
                    "{t.Envelope}",
                    "{t.TimesSent}";
                """,

            DeleteEvent = $"""
                DELETE FROM {t.QualifiedTableName}
                WHERE "{t.Id}" = @EventId;
                """
        };
    }
}
