using System.Buffers;
using System.Text;
using System.Text.Json;
using Mocha.Middlewares;
using Npgsql;
using NpgsqlTypes;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;

/// <summary>
/// Inserts and inspects outbox and scheduled message rows directly in the database.
/// </summary>
internal static class MessageRows
{
    private const string OutboxTable = "outbox_messages";
    private const string ScheduledTable = "scheduled_messages";

    public static async Task<Guid> InsertOutboxAsync(
        string connectionString,
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        const string sql =
            $"""
             INSERT INTO "{OutboxTable}" ("id", "envelope", "times_sent", "created_at")
             VALUES (@id, @envelope, 0, NOW());
             """;

        return await InsertAsync(connectionString, sql, envelope, cancellationToken);
    }

    /// <summary>
    /// Inserts an outbox row whose envelope column holds <paramref name="envelopeJson"/> as it is.
    /// </summary>
    public static async Task<Guid> InsertOutboxAsync(
        string connectionString,
        string envelopeJson,
        CancellationToken cancellationToken)
    {
        const string sql =
            $"""
             INSERT INTO "{OutboxTable}" ("id", "envelope", "times_sent", "created_at")
             VALUES (@id, @envelope, 0, NOW());
             """;

        return await InsertAsync(connectionString, sql, envelopeJson, cancellationToken);
    }

    /// <summary>
    /// Returns the attempt count of the outbox row, or <c>null</c> when the row is gone.
    /// </summary>
    public static async Task<int?> GetOutboxTimesSentAsync(
        string connectionString,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""SELECT "times_sent" FROM "{OutboxTable}" WHERE "id" = @id;""";
        command.Parameters.AddWithValue("@id", id);

        return (int?)await command.ExecuteScalarAsync(cancellationToken);
    }

    /// <summary>
    /// Waits until the outbox row has the expected attempt count and returns the last count that was read,
    /// or <c>null</c> when the row is gone.
    /// </summary>
    public static async Task<int?> WaitForOutboxTimesSentAsync(
        string connectionString,
        Guid id,
        int expected,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        var timesSent = await GetOutboxTimesSentAsync(connectionString, id, cancellationToken);

        while (timesSent != expected && timesSent is not null && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, cancellationToken);
            timesSent = await GetOutboxTimesSentAsync(connectionString, id, cancellationToken);
        }

        return timesSent;
    }

    /// <summary>
    /// Waits until the outbox row is gone and returns whether it is.
    /// </summary>
    public static async Task<bool> WaitForOutboxRemovalAsync(
        string connectionString,
        Guid id,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        var timesSent = await GetOutboxTimesSentAsync(connectionString, id, cancellationToken);

        while (timesSent is not null && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, cancellationToken);
            timesSent = await GetOutboxTimesSentAsync(connectionString, id, cancellationToken);
        }

        return timesSent is null;
    }

    public static async Task<long> CountOutboxAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""SELECT COUNT(*) FROM "{OutboxTable}";""";

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    /// <summary>
    /// Waits until the outbox holds the expected number of rows and returns the last count that was read.
    /// </summary>
    public static async Task<long> WaitForOutboxCountAsync(
        string connectionString,
        long expected,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        var count = await CountOutboxAsync(connectionString, cancellationToken);

        while (count != expected && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, cancellationToken);
            count = await CountOutboxAsync(connectionString, cancellationToken);
        }

        return count;
    }

    /// <summary>
    /// Makes every outbox row eligible for dispatch, as if it had been due for <paramref name="dueFor"/>.
    /// </summary>
    public static async Task MakeOutboxDueAsync(
        string connectionString,
        TimeSpan dueFor,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""UPDATE "{OutboxTable}" SET "created_at" = NOW() - @dueFor;""";
        command.Parameters.AddWithValue("@dueFor", dueFor);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task SetOutboxTimesSentAsync(
        string connectionString,
        Guid id,
        int timesSent,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""UPDATE "{OutboxTable}" SET "times_sent" = @timesSent WHERE "id" = @id;""";
        command.Parameters.AddWithValue("@timesSent", timesSent);
        command.Parameters.AddWithValue("@id", id);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task<long> CountScheduledAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""SELECT COUNT(*) FROM "{ScheduledTable}";""";

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public static async Task<Guid> InsertScheduledAsync(
        string connectionString,
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        const string sql =
            $"""
             INSERT INTO "{ScheduledTable}" ("id", "envelope", "scheduled_time", "times_sent", "max_attempts", "created_at")
             VALUES (@id, @envelope, NOW(), 0, 10, NOW());
             """;

        return await InsertAsync(connectionString, sql, envelope, cancellationToken);
    }

    /// <summary>
    /// Waits until the outbox row was dispatched at least twice and returns its attempt count,
    /// or returns <c>null</c> once the row is gone.
    /// </summary>
    public static Task<int?> WaitForOutboxRetryAsync(
        string connectionString,
        Guid id,
        TimeSpan timeout,
        CancellationToken cancellationToken)
        => WaitForRetryAsync(connectionString, OutboxTable, id, timeout, cancellationToken);

    /// <summary>
    /// Waits until the scheduled row was dispatched at least twice and returns its attempt count,
    /// or returns <c>null</c> once the row is gone.
    /// </summary>
    public static Task<int?> WaitForScheduledRetryAsync(
        string connectionString,
        Guid id,
        TimeSpan timeout,
        CancellationToken cancellationToken)
        => WaitForRetryAsync(connectionString, ScheduledTable, id, timeout, cancellationToken);

    private static Task<Guid> InsertAsync(
        string connectionString,
        string sql,
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
        => InsertAsync(connectionString, sql, Serialize(envelope), cancellationToken);

    private static async Task<Guid> InsertAsync(
        string connectionString,
        string sql,
        string envelopeJson,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@envelope", NpgsqlDbType.Json, envelopeJson);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return id;
    }

    private static async Task<int?> WaitForRetryAsync(
        string connectionString,
        string table,
        Guid id,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""SELECT "times_sent" FROM "{table}" WHERE "id" = @id;""";
        command.Parameters.AddWithValue("@id", id);

        int? timesSent;

        do
        {
            timesSent = (int?)await command.ExecuteScalarAsync(cancellationToken);

            if (timesSent is null or >= 2)
            {
                return timesSent;
            }

            await Task.Delay(250, cancellationToken);
        }
        while (DateTime.UtcNow < deadline);

        return timesSent;
    }

    private static string Serialize(MessageEnvelope envelope)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            var envelopeWriter = new MessageEnvelopeWriter(writer);
            envelopeWriter.WriteMessage(envelope);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
