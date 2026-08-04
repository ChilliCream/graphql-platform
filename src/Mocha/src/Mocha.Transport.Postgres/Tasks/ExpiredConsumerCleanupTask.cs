using Microsoft.Extensions.Logging;

namespace Mocha.Transport.Postgres.Tasks;

/// <summary>
/// A background task that periodically removes expired consumers from the database.
/// Consumers whose heartbeat has not been updated within the timeout period are considered
/// dead, and their CASCADE-linked temporary queues and messages are cleaned up. Runs every 60 seconds.
/// </summary>
internal sealed class ExpiredConsumerCleanupTask(
    PostgresConsumerManager consumerManager,
    PostgresConnectionManager connectionManager,
    IReadOnlyPostgresSchemaOptions schemaOptions,
    ILogger logger)
    : PostgresBackgroundTask(connectionManager, schemaOptions, logger)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromMinutes(2);

    /// <inheritdoc />
    protected override TimeSpan Interval => TimeSpan.FromSeconds(60);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await consumerManager.CleanupExpiredConsumersAsync(s_timeout, ct);
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Error, "Error cleaning up expired consumers.")]
    public static partial void ExpiredConsumerCleanupFailed(this ILogger logger, Exception exception);
}
