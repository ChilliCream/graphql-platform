using Mocha.EntityFrameworkCore.Postgres;

namespace Mocha.Scheduling;

/// <summary>
/// Holds pre-built SQL query strings for Postgres scheduled message operations, generated from
/// <see cref="ScheduledMessageTableInfo"/> column and table metadata.
/// </summary>
internal sealed class ScheduledMessageQueries
{
    /// <summary>
    /// Gets or sets the SQL statement to insert a new scheduled message into the table.
    /// </summary>
    public string InsertMessage { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL query to retrieve the earliest scheduled time that is due for dispatch.
    /// </summary>
    public string NextWakeTime { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement that locks a single scheduled message row for processing,
    /// increments the times-sent counter, and returns the id and envelope.
    /// </summary>
    public string ProcessMessage { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement to delete a dispatched scheduled message by its identifier.
    /// </summary>
    public string DeleteMessage { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement to update the last error for a scheduled message.
    /// </summary>
    public string UpdateLastError { get; set; } = null!;

    /// <summary>
    /// Creates a new <see cref="ScheduledMessageQueries"/> instance with SQL queries built from the provided table metadata.
    /// </summary>
    /// <param name="t">The scheduled message table info containing column and table names.</param>
    /// <returns>A fully initialized <see cref="ScheduledMessageQueries"/> instance.</returns>
    public static ScheduledMessageQueries From(ScheduledMessageTableInfo t)
    {
        return new ScheduledMessageQueries
        {
            InsertMessage = $"""
                INSERT INTO {t.QualifiedTableName} ("{t.Id}", "{t.Envelope}", "{t.ScheduledTime}", "{t.TimesSent}", "{t.MaxAttempts}", "{t.CreatedAt}")
                VALUES (@id, @envelope, @scheduled_time, 0, 10, NOW());
                """,

            NextWakeTime = $"""
                SELECT MIN("{t.ScheduledTime}" + INTERVAL '1 second' * POWER(2, "{t.TimesSent}")) AS "NextWakeTime"
                FROM {t.QualifiedTableName}
                WHERE "{t.TimesSent}" < "{t.MaxAttempts}";
                """,

            ProcessMessage = $"""
                UPDATE {t.QualifiedTableName}
                SET "{t.TimesSent}" = "{t.TimesSent}" + 1,
                    "{t.ScheduledTime}" = NOW() + INTERVAL '1 second' * POWER(2, "{t.TimesSent}")
                WHERE "{t.Id}" = (
                    SELECT "{t.Id}" FROM {t.QualifiedTableName}
                    WHERE "{t.TimesSent}" < "{t.MaxAttempts}"
                      AND "{t.ScheduledTime}" <= NOW()
                    ORDER BY "{t.ScheduledTime}"
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                )
                RETURNING
                    "{t.Id}",
                    "{t.Envelope}",
                    "{t.TimesSent}",
                    "{t.MaxAttempts}";
                """,

            DeleteMessage = $"""
                DELETE FROM {t.QualifiedTableName}
                WHERE "{t.Id}" = @id;
                """,

            UpdateLastError = $"""
                UPDATE {t.QualifiedTableName}
                SET "{t.LastError}" = @last_error::jsonb
                WHERE "{t.Id}" = @id;
                """
        };
    }
}
