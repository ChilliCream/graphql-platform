namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

/// <summary>
/// Creates an exchange's name for given topic.
/// </summary>
public interface IExchangeNameFactory
{
    string Create<TTopic>(TTopic topic)
        where TTopic : notnull;
}
