using System;
using RabbitMQ.Client.Events;

namespace HotChocolate.Subscriptions.RabbitMQ;

public class ActiveConsumer
{
    public AsyncEventingBasicConsumer Consumer { get; }

    public ActiveConsumer(AsyncEventingBasicConsumer consumer, Action onEmptied)
    {
        Consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        _onEmptied = onEmptied ?? throw new ArgumentNullException(nameof(onEmptied));
    }

    public Action Listen(AsyncEventHandler<BasicDeliverEventArgs> handler)
    {
        lock (_lock)
        {
            _listeners++;
            Consumer.Received += handler;
        }

        return () =>
        {
            lock (_lock)
            {
                _listeners--;

                Consumer.Received -= handler;

                if (_listeners == 0)
                    _onEmptied();
            }
        };
    }

    private int _listeners = 0;
    private readonly Action _onEmptied;
    private readonly object _lock = new();
}
