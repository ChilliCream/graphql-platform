using System.Runtime.CompilerServices;
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

    internal IDisposable? BeginLogScope(IConnection connection)
        => Logger.BeginScope(_connectionLogContexts.TryGetValue(connection, out var context)
            ? context
            : CurrentLogContext);

    internal IDisposable? BeginLogScope(IChannel channel)
        => Logger.BeginScope(_channelLogContexts.TryGetValue(channel, out var context)
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
