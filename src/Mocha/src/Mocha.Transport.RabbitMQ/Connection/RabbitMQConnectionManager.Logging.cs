using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal sealed partial class RabbitMQConnectionManager
{
    /// <summary>
    /// Gets the properties that identify the connection in structured logs: service, instance, transport,
    /// connection name and role, broker host, port and virtual host.
    /// </summary>
    public IReadOnlyDictionary<string, object?> LogContext { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Begins a logger scope that carries <see cref="LogContext"/>.
    /// </summary>
    public IDisposable? BeginLogScope() => _logger.BeginScope(LogContext);
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Debug, "Connecting RabbitMQ connection {ConnectionName} (attempt {Attempt})")]
    public static partial void ConnectionAttemptStarted(this ILogger logger, string connectionName, int attempt);

    [LoggerMessage(
        LogLevel.Information,
        "RabbitMQ connection {ConnectionName} is ready after {Elapsed} (attempt {Attempt})")]
    public static partial void ConnectionReady(
        this ILogger logger,
        string connectionName,
        int attempt,
        TimeSpan elapsed);

    [LoggerMessage(
        Message = "RabbitMQ connection {ConnectionName} is unavailable for {Elapsed}: attempt {Attempt} failed. "
            + "Retrying in {Delay}")]
    public static partial void ConnectionAttemptFailed(
        this ILogger logger,
        Exception exception,
        LogLevel level,
        string connectionName,
        int attempt,
        TimeSpan elapsed,
        TimeSpan delay);

    [LoggerMessage(
        LogLevel.Error,
        "RabbitMQ connection {ConnectionName} could not be established after {Elapsed} and {Attempt} attempts. "
            + "The start fails")]
    public static partial void ConnectionFailed(
        this ILogger logger,
        Exception exception,
        string connectionName,
        int attempt,
        TimeSpan elapsed);

    [LoggerMessage(
        LogLevel.Warning,
        "RabbitMQ connection {ConnectionName} was lost: {ReplyCode} {ReplyText} (initiator: {Initiator}, "
            + "cause: {Cause}). Reconnecting in the background")]
    public static partial void ConnectionLost(
        this ILogger logger,
        string connectionName,
        ushort replyCode,
        string? replyText,
        ShutdownInitiator initiator,
        string? cause);

    [LoggerMessage(LogLevel.Warning, "Closing RabbitMQ connection {ConnectionName}: {Reason}")]
    public static partial void ConnectionAborted(this ILogger logger, string connectionName, string reason);

    [LoggerMessage(LogLevel.Information, "RabbitMQ connection {ConnectionName} is closed")]
    public static partial void ConnectionStopped(this ILogger logger, string connectionName);

    [LoggerMessage(
        LogLevel.Warning,
        "RabbitMQ connection {ConnectionName} is blocked by the broker: {Reason}. Publishes wait until the "
            + "broker unblocks it")]
    public static partial void ConnectionBlocked(this ILogger logger, string connectionName, string reason);

    [LoggerMessage(
        LogLevel.Information,
        "RabbitMQ connection {ConnectionName} was unblocked by the broker after {Elapsed}")]
    public static partial void ConnectionUnblocked(this ILogger logger, string connectionName, TimeSpan elapsed);

    [LoggerMessage(LogLevel.Debug, "Closing a discarded RabbitMQ connection of {ConnectionName} failed")]
    public static partial void ConnectionCloseFailed(this ILogger logger, Exception exception, string connectionName);

    [LoggerMessage(LogLevel.Error, "A callback of RabbitMQ connection {ConnectionName} failed. Detail: {Detail}")]
    public static partial void ConnectionCallbackFailed(
        this ILogger logger,
        Exception exception,
        string connectionName,
        string detail);
}
