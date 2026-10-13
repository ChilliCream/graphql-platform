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

    private static async Task<Guid> InsertAsync(
        string connectionString,
        string sql,
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@envelope", NpgsqlDbType.Json, Serialize(envelope));

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
