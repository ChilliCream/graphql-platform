using HotChocolate.Subscriptions.Diagnostics;
using static HotChocolate.Subscriptions.Postgres.PostgresResources;

namespace HotChocolate.Subscriptions.Postgres;

internal sealed class PostgresTopic<T> : DefaultTopic<T>
{
    private readonly IMessageSerializer _serializer;
    private readonly PostgresChannel _channel;

    /// <inheritdoc />
    public PostgresTopic(
        string name,
        int capacity,
        IMessageSerializer serializer,
        PostgresChannel channel,
        TopicBufferFullMode fullMode,
        ISubscriptionDiagnosticEvents diagnosticEvents)
        : base(name, capacity, fullMode, diagnosticEvents)
    {
        _serializer = serializer;
        _channel = channel;
    }

    /// <inheritdoc />
    protected override async ValueTask<IDisposable> OnConnectAsync(
        CancellationToken cancellationToken)
    {
        await _channel.EnsureInitialized(cancellationToken);

        return _channel.Subscribe(new PostgresChannelObserver(Name, Dispatch));
    }

    private void Dispatch(string message) => DispatchMessage(_serializer, message);
}
