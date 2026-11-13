using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Debug, "RabbitMQ {ConnectionRole} connection connecting (attempt {Attempt})")]
    public static partial void ConnectionAttemptStarted(this ILogger logger, string connectionRole, int attempt);

    [LoggerMessage(
        LogLevel.Information,
        "RabbitMQ {ConnectionRole} connection ready after {Elapsed} (attempt {Attempt})")]
    public static partial void ConnectionReady(
        this ILogger logger,
        string connectionRole,
        int attempt,
        TimeSpan elapsed);

    [LoggerMessage(
        Message = "RabbitMQ {ConnectionRole} connection unavailable for {Elapsed}, attempt {Attempt} failed; "
            + "retrying in {Delay}")]
    public static partial void ConnectionAttemptFailed(
        this ILogger logger,
        Exception exception,
        LogLevel level,
        string connectionRole,
        int attempt,
        TimeSpan elapsed,
        TimeSpan delay);

    [LoggerMessage(
        LogLevel.Error,
        "RabbitMQ {ConnectionRole} connection not ready after {Elapsed} and {Attempt} attempts; the start fails")]
    public static partial void ConnectionFailed(
        this ILogger logger,
        Exception exception,
        string connectionRole,
        int attempt,
        TimeSpan elapsed);

    [LoggerMessage(
        LogLevel.Warning,
        "RabbitMQ {ConnectionRole} connection lost: {ReplyCode} {ReplyText} (initiator {Initiator}, cause "
            + "{Cause}); reconnecting")]
    public static partial void ConnectionLost(
        this ILogger logger,
        string connectionRole,
        ushort replyCode,
        string? replyText,
        ShutdownInitiator initiator,
        string cause);

    [LoggerMessage(LogLevel.Warning, "RabbitMQ {ConnectionRole} connection closed by Mocha: {Reason}; reconnecting")]
    public static partial void ConnectionAborted(this ILogger logger, string connectionRole, string reason);

    [LoggerMessage(LogLevel.Information, "RabbitMQ {ConnectionRole} connection closed")]
    public static partial void ConnectionStopped(this ILogger logger, string connectionRole);

    [LoggerMessage(
        LogLevel.Warning,
        "RabbitMQ {ConnectionRole} connection blocked by the broker: {Reason}; publishes wait")]
    public static partial void ConnectionBlocked(this ILogger logger, string connectionRole, string reason);

    [LoggerMessage(LogLevel.Information, "RabbitMQ {ConnectionRole} connection unblocked after {Elapsed}")]
    public static partial void ConnectionUnblocked(this ILogger logger, string connectionRole, TimeSpan elapsed);

    [LoggerMessage(LogLevel.Debug, "RabbitMQ {ConnectionRole} connection: closing a discarded connection failed")]
    public static partial void ConnectionCloseFailed(this ILogger logger, Exception exception, string connectionRole);

    [LoggerMessage(LogLevel.Error, "RabbitMQ {ConnectionRole} connection: a callback failed ({Detail})")]
    public static partial void ConnectionCallbackFailed(
        this ILogger logger,
        Exception exception,
        string connectionRole,
        string detail);
}
