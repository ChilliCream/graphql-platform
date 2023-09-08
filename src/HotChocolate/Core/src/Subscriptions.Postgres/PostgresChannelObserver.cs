namespace HotChocolate.Subscriptions.Postgres;

internal sealed class PostgresChannelObserver
{
    private readonly Action<string> _onMessage;

    public PostgresChannelObserver(string topic, Action<string> onMessage)
    {
        Topic = topic;
        _onMessage = onMessage;
    }

    public string Topic { get; }

    public void OnNext(string message) => _onMessage(message);
}
