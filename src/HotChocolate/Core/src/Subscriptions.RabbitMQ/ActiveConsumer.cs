using System;
using RabbitMQ.Client.Events;

namespace HotChocolate.Subscriptions.RabbitMQ;

public class ActiveConsumer
{
    private int _listeners;
    private readonly Action _onEmpited;
    private readonly object _lock = new();


    public ActiveConsumer(AsyncEventingBasicConsumer consumer, Action onEmpited)
    {
        Consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        _onEmpited = onEmpited ?? throw new ArgumentNullException(nameof(onEmpited));
    }

    public AsyncEventingBasicConsumer Consumer { get; }

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
                {
                    _onEmpited();
                }
            }
        };
    }
}
