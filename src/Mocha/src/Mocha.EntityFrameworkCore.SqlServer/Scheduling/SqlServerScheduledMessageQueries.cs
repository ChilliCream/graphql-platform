using Mocha.EntityFrameworkCore.SqlServer;

namespace Mocha.Scheduling;

/// <summary>
/// Holds pre-built T-SQL query strings for SQL Server scheduled message operations, generated from
/// <see cref="ScheduledMessageTableInfo"/> column and table metadata.
/// </summary>
internal sealed class SqlServerScheduledMessageQueries
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
    /// Gets or sets the SQL statement to cancel a scheduled message by deleting it if it has not
    /// exceeded its maximum delivery attempts.
    /// </summary>
    public string CancelMessage { get; set; } = null!;

    /// <summary>
    /// Creates a new <see cref="SqlServerScheduledMessageQueries"/> instance with T-SQL queries built from the provided table metadata.
    /// </summary>
    /// <param name="t">The scheduled message table info containing column and table names.</param>
    /// <returns>A fully initialized <see cref="SqlServerScheduledMessageQueries"/> instance.</returns>
    public static SqlServerScheduledMessageQueries From(ScheduledMessageTableInfo t)
    {
        return new SqlServerScheduledMessageQueries
        {
            InsertMessage = $"""
                INSERT INTO {t.QualifiedTableName} ([{t.Id}], [{t.Envelope}], [{t.ScheduledTime}], [{t.TimesSent}], [{t.MaxAttempts}], [{t.CreatedAt}])
                VALUES (@id, @envelope, @scheduled_time, 0, 10, SYSUTCDATETIME());
                """,

            NextWakeTime = $"""
                SELECT MIN(DATEADD(SECOND, POWER(2, [{t.TimesSent}]), [{t.ScheduledTime}])) AS NextWakeTime
                FROM {t.QualifiedTableName}
                WHERE [{t.TimesSent}] < [{t.MaxAttempts}];
                """,

            ProcessMessage = $"""
                UPDATE {t.QualifiedTableName}
                SET [{t.TimesSent}] = [{t.TimesSent}] + 1,
                    [{t.ScheduledTime}] = DATEADD(SECOND, POWER(2, [{t.TimesSent}]), SYSUTCDATETIME())
                OUTPUT inserted.[{t.Id}], inserted.[{t.Envelope}], inserted.[{t.TimesSent}], inserted.[{t.MaxAttempts}]
                WHERE [{t.Id}] = (
                    SELECT TOP(1) [{t.Id}]
                    FROM {t.QualifiedTableName} WITH (UPDLOCK, ROWLOCK, READPAST)
                    WHERE [{t.TimesSent}] < [{t.MaxAttempts}] AND [{t.ScheduledTime}] <= SYSUTCDATETIME()
                    ORDER BY [{t.ScheduledTime}]
                );
                """,

            DeleteMessage = $"""
                DELETE FROM {t.QualifiedTableName} WHERE [{t.Id}] = @id;
                """,

            UpdateLastError = $"""
                UPDATE {t.QualifiedTableName} SET [{t.LastError}] = @last_error WHERE [{t.Id}] = @id;
                """,

            CancelMessage = $"""
                DELETE FROM {t.QualifiedTableName}
                OUTPUT deleted.[{t.Id}]
                WHERE [{t.Id}] = @id AND [{t.TimesSent}] < [{t.MaxAttempts}];
                """
        };
    }
}
