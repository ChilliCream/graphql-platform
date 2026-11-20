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
    /// The maximum number of messages of one transaction that are dispatched in one batch.
    /// </summary>
    public const int BatchSize = 100;

    /// <summary>
    /// Gets or sets the SQL statement to insert a new message envelope into the outbox table.
    /// </summary>
    public string InsertEnvelope { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL query that returns the number of seconds until the first message of the
    /// earliest retryable transaction is due, or <c>NULL</c> if there is none.
    /// </summary>
    public string NextPollingInterval { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement that locks the first message of one due transaction, counts an
    /// attempt for it, and returns up to <see cref="BatchSize"/> messages of that transaction in identifier order.
    /// </summary>
    public string ProcessEvent { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement to delete a processed outbox message by its identifier.
    /// </summary>
    public string DeleteEvent { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement that deletes the messages of a transaction up to a message identifier.
    /// </summary>
    public string DeleteTransactionEvents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement that counts a failed attempt for a message and sets its next due time.
    /// </summary>
    public string RescheduleEvent { get; set; } = null!;

    /// <summary>
    /// Creates a new <see cref="PostgresMessageOutboxQueries"/> instance with SQL queries built from the provided table metadata.
    /// </summary>
    /// <param name="t">The outbox table info containing column and table names.</param>
    /// <returns>A fully initialized <see cref="PostgresMessageOutboxQueries"/> instance.</returns>
    public static PostgresMessageOutboxQueries From(OutboxTableInfo t)
    {
        var table = t.QualifiedTableName;

        // The attempt limit and the retry delay are defined only in these two fragments.
        var canRetry = $"""m."{t.TimesSent}" < {MaxAttempts}""";
        var recordAttempt = $"""
            "{t.TimesSent}" = "{t.TimesSent}" + 1,
            "{t.CreatedAt}" = NOW() + INTERVAL '1 second' * POWER(2, "{t.TimesSent}")
            """;

        // A row is the first of its transaction when no row with the same transaction id has a smaller id.
        var isFirstOfTransaction = $"""
            (m."{t.TransactionId}" IS NULL OR NOT EXISTS (
                SELECT 1 FROM {table} e
                WHERE e."{t.TransactionId}" = m."{t.TransactionId}" AND e."{t.Id}" < m."{t.Id}"))
            """;

        return new PostgresMessageOutboxQueries
        {
            InsertEnvelope = $"""
                INSERT INTO {table} ("{t.Id}", "{t.Envelope}", "{t.TimesSent}", "{t.CreatedAt}", "{t.TransactionId}")
                VALUES (@id, @envelope, 0, NOW(), @transactionId);
                """,

            NextPollingInterval = $"""
                SELECT EXTRACT(EPOCH FROM (MIN(m."{t.CreatedAt}") - NOW()))::double precision
                FROM {table} m
                WHERE {canRetry} AND {isFirstOfTransaction};
                """,

            ProcessEvent = $"""
                WITH head AS (
                    UPDATE {table}
                    SET {recordAttempt}
                    WHERE "{t.Id}" = (
                        SELECT m."{t.Id}" FROM {table} m
                        WHERE {canRetry} AND m."{t.CreatedAt}" <= NOW() AND {isFirstOfTransaction}
                        ORDER BY m."{t.CreatedAt}", m."{t.Id}"
                        FOR UPDATE OF m SKIP LOCKED
                        LIMIT 1
                    )
                    RETURNING "{t.Id}", "{t.TransactionId}", "{t.TimesSent}"
                )
                SELECT m."{t.Id}", m."{t.Envelope}", h."{t.TransactionId}", h."{t.TimesSent}"
                FROM head h
                JOIN {table} m ON m."{t.Id}" = h."{t.Id}"
                UNION ALL
                SELECT m."{t.Id}", m."{t.Envelope}", h."{t.TransactionId}", m."{t.TimesSent}" + 1
                FROM head h
                JOIN {table} m ON m."{t.TransactionId}" = h."{t.TransactionId}" AND m."{t.Id}" > h."{t.Id}"
                ORDER BY 1
                LIMIT {BatchSize};
                """,

            DeleteEvent = $"""
                DELETE FROM {table}
                WHERE "{t.Id}" = @EventId;
                """,

            DeleteTransactionEvents = $"""
                DELETE FROM {table}
                WHERE "{t.TransactionId}" = @TransactionId AND "{t.Id}" <= @LastEventId;
                """,

            RescheduleEvent = $"""
                UPDATE {table}
                SET {recordAttempt}
                WHERE "{t.Id}" = @EventId;
                """
        };
    }
}
