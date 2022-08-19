namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

public class QueueNameFactory: IQueueNameFactory
{
    public string Create(string exchangeName, string instanceName)
        => $"{instanceName} - {exchangeName}";
}
