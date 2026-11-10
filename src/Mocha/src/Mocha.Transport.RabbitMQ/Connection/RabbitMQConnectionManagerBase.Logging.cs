using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

public abstract partial class RabbitMQConnectionManagerBase
{
    private readonly ConditionalWeakTable<IConnection, Dictionary<string, object?>> _connectionLogContexts = new();
    private readonly ConditionalWeakTable<IChannel, Dictionary<string, object?>> _channelLogContexts = new();
    private IReadOnlyDictionary<string, object?>? _currentLogContext;

    internal IReadOnlyDictionary<string, object?> LogContext { get; init; } = new Dictionary<string, object?>();

    private IReadOnlyDictionary<string, object?> CurrentLogContext => _currentLogContext ?? LogContext;

    internal IDisposable? BeginLogScope() => Logger.BeginScope(CurrentLogContext);

    internal IDisposable? BeginLogScope(IConnection? connection)
        => Logger.BeginScope(connection is not null && _connectionLogContexts.TryGetValue(connection, out var context)
            ? context
            : CurrentLogContext);

    internal IDisposable? BeginLogScope(IChannel? channel)
        => Logger.BeginScope(channel is not null && _channelLogContexts.TryGetValue(channel, out var context)
            ? context
            : CurrentLogContext);

    internal void TrackChannel(IConnection connection, IChannel channel)
        => _channelLogContexts.GetValue(channel, _ =>
        {
            var context = _connectionLogContexts.TryGetValue(connection, out var connectionContext)
                ? connectionContext
                : CurrentLogContext;
            return new Dictionary<string, object?>(context) { ["ChannelNumber"] = channel.ChannelNumber };
        });

    private void StartConnectionLogContext()
        => _currentLogContext = new Dictionary<string, object?>(LogContext)
        {
            ["ConnectionName"] = ConnectionName,
            ["ConnectionId"] = Guid.NewGuid()
        };

    private void TrackConnection(IConnection connection)
    {
        var context = new Dictionary<string, object?>(CurrentLogContext)
        {
            ["ClientProvidedName"] = connection.ClientProvidedName
        };
        if (connection.Endpoint is { } endpoint)
        {
            context["BrokerHost"] = endpoint.HostName;
            context["BrokerPort"] = endpoint.Port;
        }

        _currentLogContext = _connectionLogContexts.GetValue(connection, _ => context);
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Debug, "Creating RabbitMQ connection {ConnectionName}")]
    public static partial void CreatingConnectionUsingFactoryDelegate(this ILogger logger, string connectionName);

    [LoggerMessage(LogLevel.Debug, "RabbitMQ connection {ConnectionName} is open with client name {ClientProvidedName}; setup is pending")]
    public static partial void ConnectionOpened(this ILogger logger, string connectionName, string? clientProvidedName);

    [LoggerMessage(LogLevel.Information, "RabbitMQ connection {ConnectionName} is ready after {Elapsed}")]
    public static partial void ConnectionReady(this ILogger logger, string connectionName, TimeSpan elapsed);

    [LoggerMessage(LogLevel.Information, "RabbitMQ connection {ConnectionName} recovered after {Elapsed}")]
    public static partial void ConnectionRecovered(this ILogger logger, string connectionName, TimeSpan elapsed);

    [LoggerMessage(LogLevel.Error, "Failed to establish RabbitMQ connection {ConnectionName} after {Attempts} attempts")]
    public static partial void FailedToCreateConnectionAfterAttempts(
        this ILogger logger,
        Exception ex,
        string connectionName,
        int attempts);

    [LoggerMessage(Message = "Failed to establish RabbitMQ connection {ConnectionName} (attempt {Attempt}). Retrying in {Delay}")]
    public static partial void FailedToCreateConnectionRetrying(
        this ILogger logger,
        Exception ex,
        LogLevel level,
        string connectionName,
        int attempt,
        TimeSpan delay);

    [LoggerMessage(LogLevel.Debug, "RabbitMQ connection {ConnectionName} closed by application")]
    public static partial void ConnectionClosedByApplication(this ILogger logger, string connectionName);

    [LoggerMessage(LogLevel.Information, "RabbitMQ connection manager {ConnectionName} stopped")]
    public static partial void ConnectionManagerStopped(this ILogger logger, string connectionName);

    [LoggerMessage(LogLevel.Warning, "RabbitMQ connection {ConnectionName} lost. Initiator: {Initiator}, Reply: {ReplyCode} {ReplyText}")]
    public static partial void ConnectionShutdownDetected(
        this ILogger logger,
        string connectionName,
        ShutdownInitiator initiator,
        ushort replyCode,
        string replyText);

    [LoggerMessage(LogLevel.Error, "Error handling loss of RabbitMQ connection {ConnectionName}")]
    public static partial void ErrorHandlingConnectionLoss(this ILogger logger, Exception ex, string connectionName);

    [LoggerMessage(LogLevel.Warning, "RabbitMQ connection {ConnectionName} blocked by broker: {Reason}")]
    public static partial void ConnectionBlocked(this ILogger logger, string connectionName, string reason);

    [LoggerMessage(LogLevel.Information, "RabbitMQ connection {ConnectionName} unblocked by broker")]
    public static partial void ConnectionUnblocked(this ILogger logger, string connectionName);

    [LoggerMessage(Message = "RabbitMQ connection {ConnectionName} is still unavailable after {Elapsed} (attempt {Attempt}). Retrying in {Delay}")]
    public static partial void ReconnectionAttemptFailed(
        this ILogger logger,
        Exception ex,
        LogLevel level,
        string connectionName,
        int attempt,
        TimeSpan delay,
        TimeSpan elapsed);

    [LoggerMessage(LogLevel.Error, "Error disposing RabbitMQ connection {ConnectionName}")]
    public static partial void ErrorDisposingConnection(this ILogger logger, Exception ex, string connectionName);

    [LoggerMessage(LogLevel.Error, "Exception in RabbitMQ connection {ConnectionName} callback. Detail: {Detail}")]
    public static partial void ExceptionInConnectionCallback(
        this ILogger logger,
        Exception ex,
        string connectionName,
        string detail);
}
