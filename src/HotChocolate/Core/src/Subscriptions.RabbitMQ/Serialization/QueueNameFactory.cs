using System;

namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

public class QueueNameFactory: IQueueNameFactory
{
    public string Create(string exchangeName, string instanceName)
    {
        if (exchangeName is null) throw new ArgumentNullException(nameof(exchangeName));
        if (instanceName is null) throw new ArgumentNullException(nameof(instanceName));

        return $"{instanceName} - {exchangeName}";
    }
}
