using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Writes the log entries about the lifecycle of one RabbitMQ connection.
/// </summary>
/// <param name="logger">The logger that receives the entries.</param>
/// <param name="connectionRole">The role of the connection, which names it in each entry.</param>
internal sealed partial class RabbitMQConnectionLogger(ILogger logger, string connectionRole)
{
    public void ConnectionAttemptStarted(int attempt) => ConnectionAttemptStarted(connectionRole, attempt);

    public void ConnectionReady(int attempt, TimeSpan elapsed) => ConnectionReady(connectionRole, attempt, elapsed);

    public void ConnectionAttemptFailed(Exception exception, int attempt, TimeSpan elapsed, TimeSpan delay)
        => ConnectionAttemptFailed(
            exception,
            exception.IsPermanentFailure() ? LogLevel.Error : LogLevel.Warning,
            connectionRole,
            attempt,
            elapsed,
            delay);

    public void ConnectionFailed(Exception exception, int attempt, TimeSpan elapsed)
        => ConnectionFailed(exception, connectionRole, attempt, elapsed);

    public void ConnectionLost(ShutdownEventArgs? reason)
    {
        // The client repeats the reply text as the cause when the socket failed.
        var cause = reason?.Exception?.Message;
        ConnectionLost(
            connectionRole,
            reason?.ReplyCode ?? 0,
            reason?.ReplyText,
            reason?.Initiator ?? ShutdownInitiator.Library,
            cause is null || cause == reason?.ReplyText ? "none" : cause);
    }

    public void ConnectionAborted(string reason) => ConnectionAborted(connectionRole, reason);

    public void ConnectionStopped() => ConnectionStopped(connectionRole);

    public void ConnectionBlocked(string reason) => ConnectionBlocked(connectionRole, reason);

    public void ConnectionUnblocked(TimeSpan elapsed) => ConnectionUnblocked(connectionRole, elapsed);

    public void ConnectionCloseFailed(Exception exception) => ConnectionCloseFailed(exception, connectionRole);

    public void ConnectionCallbackFailed(Exception exception, IDictionary<string, object>? detail)
    {
        var text = new StringBuilder();
        foreach (var item in detail ?? new Dictionary<string, object>())
        {
            if (text.Length > 0)
            {
                text.Append(", ");
            }

            text.Append(item.Key).Append(": ").Append(item.Value);
        }

        ConnectionCallbackFailed(exception, connectionRole, text.ToString());
    }

    [LoggerMessage(LogLevel.Debug, "RabbitMQ {ConnectionRole} connection connecting (attempt {Attempt})")]
    private partial void ConnectionAttemptStarted(string connectionRole, int attempt);

    [LoggerMessage(
        LogLevel.Information,
        "RabbitMQ {ConnectionRole} connection ready after {Elapsed} (attempt {Attempt})")]
    private partial void ConnectionReady(string connectionRole, int attempt, TimeSpan elapsed);

    [LoggerMessage(
        Message = "RabbitMQ {ConnectionRole} connection unavailable for {Elapsed}, attempt {Attempt} failed; "
            + "retrying in {Delay}")]
    private partial void ConnectionAttemptFailed(
        Exception exception,
        LogLevel level,
        string connectionRole,
        int attempt,
        TimeSpan elapsed,
        TimeSpan delay);

    [LoggerMessage(
        LogLevel.Error,
        "RabbitMQ {ConnectionRole} connection not ready after {Elapsed} and {Attempt} attempts; the start fails")]
    private partial void ConnectionFailed(Exception exception, string connectionRole, int attempt, TimeSpan elapsed);

    [LoggerMessage(
        LogLevel.Warning,
        "RabbitMQ {ConnectionRole} connection lost: {ReplyCode} {ReplyText} (initiator {Initiator}, cause "
            + "{Cause}); reconnecting")]
    private partial void ConnectionLost(
        string connectionRole,
        ushort replyCode,
        string? replyText,
        ShutdownInitiator initiator,
        string cause);

    [LoggerMessage(LogLevel.Warning, "RabbitMQ {ConnectionRole} connection closed by Mocha: {Reason}; reconnecting")]
    private partial void ConnectionAborted(string connectionRole, string reason);

    [LoggerMessage(LogLevel.Information, "RabbitMQ {ConnectionRole} connection closed")]
    private partial void ConnectionStopped(string connectionRole);

    [LoggerMessage(
        LogLevel.Warning,
        "RabbitMQ {ConnectionRole} connection blocked by the broker: {Reason}; publishes wait")]
    private partial void ConnectionBlocked(string connectionRole, string reason);

    [LoggerMessage(LogLevel.Information, "RabbitMQ {ConnectionRole} connection unblocked after {Elapsed}")]
    private partial void ConnectionUnblocked(string connectionRole, TimeSpan elapsed);

    [LoggerMessage(LogLevel.Debug, "RabbitMQ {ConnectionRole} connection: closing a discarded connection failed")]
    private partial void ConnectionCloseFailed(Exception exception, string connectionRole);

    [LoggerMessage(LogLevel.Error, "RabbitMQ {ConnectionRole} connection: a callback failed ({Detail})")]
    private partial void ConnectionCallbackFailed(Exception exception, string connectionRole, string detail);
}
