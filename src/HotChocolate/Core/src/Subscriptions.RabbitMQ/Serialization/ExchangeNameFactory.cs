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
        if (topic is string stringTopic)
            return stringTopic;

        string topicType = topic.GetType().FullName ?? throw new NullReferenceException("Topic type has to have a name!");
        string topicObj = _serializer.Serialize(topic);

        return $"{topicType}: {topicObj}";
    }

    private readonly ISerializer _serializer;
}
