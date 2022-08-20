namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

/// <summary>
/// Names a queue that will bind together this instnace of HC and exchange of given name.
/// </summary>
public interface IQueueNameFactory
{
    public string Create(string exchangeName, string instanceName);
}
