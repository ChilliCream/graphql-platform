namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

public interface IQueueNameFactory
{
    public string Create(string exchangeName, string instanceName);
}
