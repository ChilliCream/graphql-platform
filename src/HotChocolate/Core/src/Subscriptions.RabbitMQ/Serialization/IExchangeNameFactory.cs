namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

public interface IExchangeNameFactory
{
    string Create<TTopic>(TTopic topic)
        where TTopic : notnull;
}
