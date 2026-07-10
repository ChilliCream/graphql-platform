using Microsoft.Extensions.Logging;

namespace Mocha.Transport.Postgres.Tasks;

/// <summary>
/// A background task that periodically deletes expired messages that have not been
/// picked up by a consumer. Runs every 60 seconds.
/// </summary>
internal sealed class MessageCleanupTask(
    PostgresConnectionManager connectionManager,
    IReadOnlyPostgresSchemaOptions schemaOptions,
    ILogger logger)
    : PostgresBackgroundTask(connectionManager, schemaOptions, logger)
{
    /// <inheritdoc />
    protected override TimeSpan Interval => TimeSpan.FromSeconds(60);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await using var connection = await ConnectionManager.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            DELETE FROM {SchemaOptions.MessageTable}
            WHERE expiration_time IS NOT NULL
              AND expiration_time < now()
              AND consumer_id IS NULL
            """;

        var deleted = await command.ExecuteNonQueryAsync(ct);

        if (deleted > 0)
        {
            Logger.ExpiredMessagesCleanup(deleted);
        }
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Information, "Message cleanup removed {DeletedCount} expired messages.")]
    public static partial void ExpiredMessagesCleanup(this ILogger logger, int deletedCount);
}
