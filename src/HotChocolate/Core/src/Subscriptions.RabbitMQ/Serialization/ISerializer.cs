namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

public interface ISerializer
{
    public string Serialize<TValue>(TValue value);

    public TValue Deserialize<TValue>(string value);
}
