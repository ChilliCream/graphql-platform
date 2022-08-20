using System;

namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

public class ExchangeNameFactory: IExchangeNameFactory
{
    public ExchangeNameFactory(ISerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public string Create<TTopic>(TTopic topic)
        where TTopic: notnull
    {
        if (topic is null) throw new ArgumentNullException(nameof(topic));
        return _serializer.SerializeOrString(topic);
    }

    private readonly ISerializer _serializer;
}
