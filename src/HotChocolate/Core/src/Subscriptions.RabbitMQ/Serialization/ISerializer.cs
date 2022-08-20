namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

/// <summary>
/// Serialization used by all components of RabbitMQ PubSub.
/// </summary>
public interface ISerializer
{
    public string Serialize<TValue>(TValue value);

    public TValue Deserialize<TValue>(string value);
}
