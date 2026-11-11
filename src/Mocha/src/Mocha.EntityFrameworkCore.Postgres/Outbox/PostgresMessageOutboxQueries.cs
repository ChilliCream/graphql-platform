using System.Globalization;
using Mocha.EntityFrameworkCore.Postgres;

namespace Mocha.Outbox;

/// <summary>
/// Holds pre-built SQL query strings for Postgres outbox operations, generated from
/// <see cref="OutboxTableInfo"/> column and table metadata.
/// </summary>
internal sealed class PostgresMessageOutboxQueries
{
    // Two to the power of 30 seconds is about 34 years, and a larger exponent would overflow for a high
    // attempt count.
    private const int MaxBackoffExponent = 30;

    /// <summary>
    /// Gets or sets the SQL statement to insert a new message envelope into the outbox table.
    /// </summary>
    public string InsertEnvelope { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL query that returns the seconds until the earliest pending message is due. The
    /// result is not positive when a message is due, and null when no message is pending.
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
    /// <param name="options">The outbox options that supply the attempt limit and the retry delay cap.</param>
    /// <returns>A fully initialized <see cref="PostgresMessageOutboxQueries"/> instance.</returns>
    public static PostgresMessageOutboxQueries From(OutboxTableInfo t, PostgresOutboxOptions options)
    {
        var maxAttempts = options.MaxAttempts.ToString(CultureInfo.InvariantCulture);
        var maxRetryDelay = options.MaxRetryDelay.TotalSeconds.ToString("R", CultureInfo.InvariantCulture);
        var retryDelay =
            $"""
            INTERVAL '1 second' * LEAST(POWER(2, LEAST("{t.TimesSent}", {MaxBackoffExponent})), {maxRetryDelay})
            """;

        return new PostgresMessageOutboxQueries
        {
            InsertEnvelope = $"""
                INSERT INTO {t.QualifiedTableName} ("{t.Id}", "{t.Envelope}", "{t.TimesSent}", "{t.CreatedAt}")
                VALUES (@id, @envelope, 0, NOW());
                """,

            NextPollingInterval = $"""
                SELECT (EXTRACT(EPOCH FROM MIN("{t.CreatedAt}")) - EXTRACT(EPOCH FROM NOW()))::double precision
                    AS "SecondsUntilDue"
                FROM {t.QualifiedTableName}
                WHERE "{t.TimesSent}" < {maxAttempts};
                """,

            ProcessEvent = $"""
                UPDATE {t.QualifiedTableName}
                SET "{t.TimesSent}" = "{t.TimesSent}" + 1,
                    "{t.CreatedAt}" = NOW() + {retryDelay}
                WHERE "{t.Id}" = (
                    SELECT "{t.Id}" FROM {t.QualifiedTableName}
                    WHERE "{t.TimesSent}" < {maxAttempts} AND "{t.CreatedAt}" <= NOW()
                    ORDER BY "{t.CreatedAt}", "{t.Id}"
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
