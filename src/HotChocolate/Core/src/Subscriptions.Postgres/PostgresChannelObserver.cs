namespace HotChocolate.Subscriptions.Postgres;

internal sealed class PostgresChannelObserver
{
    private readonly string _topic;
    private readonly Action<string> _onMessage;

    public PostgresChannelObserver(string topic, Action<string> onMessage)
    {
        _topic = topic;
        _onMessage = onMessage;
    }

    public void OnNext(ref PostgresMessageEnvelope value)
    {
        if (value.Topic == _topic)
        {
            _onMessage(value.Payload);
        }
    }
}
