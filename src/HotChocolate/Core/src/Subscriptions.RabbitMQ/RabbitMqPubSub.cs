using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Subscriptions.RabbitMQ.Configuration;
using HotChocolate.Subscriptions.RabbitMQ.Consts;
using HotChocolate.Subscriptions.RabbitMQ.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HotChocolate.Subscriptions.RabbitMQ;

public class RabbitMqPubSub
    : ITopicEventReceiver
    , ITopicEventSender
{
    public RabbitMqPubSub(IModel channel, Config configuration, IExchangeNameFactory exchangeNameFactory,
        IQueueNameFactory queueNameFactory, ISerializer serializer)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _exchangeNameFactory = exchangeNameFactory ?? throw new ArgumentNullException(nameof(exchangeNameFactory));
        _queueNameFactory = queueNameFactory ?? throw new ArgumentNullException(nameof(queueNameFactory));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public ValueTask<ISourceStream<TMessage>> SubscribeAsync<TTopic, TMessage>(TTopic topic, CancellationToken token = default) where TTopic : notnull
    {
        string exchangeName = _exchangeNameFactory.Create(topic);
        string queueName = _queueNameFactory.Create(exchangeName, _configuration.InstanceName);

        TryDeclareExchange(exchangeName);
        TryDeclareAndBindQueue(queueName, exchangeName);
        
        ActiveConsumer consumer = _activeConsumers.GetOrAdd(queueName, newQueue =>
        {
            ActiveConsumer activeConsumer = new(new AsyncEventingBasicConsumer(_channel), () => TryDeleteConsumer(queueName));
            _channel.BasicConsume(newQueue, false, activeConsumer.Consumer);
            return activeConsumer;
        });
        
        RabbitMqEventStream<TMessage> stream = new(_serializer, consumer);
        return new(stream);
    }

    public ValueTask SendAsync<TTopic, TMessage>(TTopic topic, TMessage message, CancellationToken token = default) where TTopic : notnull
    {
        string exchangeName = _exchangeNameFactory.Create(topic);
        TryDeclareExchange(exchangeName);

        _configuration.PublishMessage(_channel, exchangeName, _serializer.Serialize(message));

        return new ValueTask();
    }

    public ValueTask CompleteAsync<TTopic>(TTopic topic) where TTopic : notnull
    {
        string exchangeName = _exchangeNameFactory.Create(topic);
        TryDeclareExchange(exchangeName);

        _configuration.PublishMessage(_channel, exchangeName, WellKnownMessages.Completed);

        return new ValueTask();
    }
    
    private readonly HashSet<string> _declaredExchanges = new();
    private readonly HashSet<string> _declaredQueues = new();
    private readonly ConcurrentDictionary<string, ActiveConsumer> _activeConsumers = new();
    private readonly object _lock = new ();
    
    private readonly IModel _channel;
    private readonly Config _configuration;
    private readonly IExchangeNameFactory _exchangeNameFactory;
    private readonly IQueueNameFactory _queueNameFactory;
    private readonly ISerializer _serializer;
    
    private void TryDeleteConsumer(string forQueueName)
    {
        lock (_lock)
        {
            if (!_configuration.DisposeUnusedConsumers)
                return;

            if (_activeConsumers.TryRemove(forQueueName, out ActiveConsumer? consumer))
                // As we are using max one consumer per queue, there will be only one tag
                _channel.BasicCancel(consumer.Consumer.ConsumerTags.Single());
        }
    }

    private void TryDeclareExchange(string name)
    {
        lock (_lock)
        {
            if (!_declaredExchanges.Contains(name))
            {
                _configuration.DeclareExchange(_channel, name);
                _declaredExchanges.Add(name);
            }
        }
    }

    private void TryDeclareAndBindQueue(string name, string exchangeName)
    {
        lock (_lock)
        {
            if (!_declaredQueues.Contains(name))
            {
                _configuration.DeclareQueue(_channel, name);
                _configuration.BindQueue(_channel, exchangeName, name);
                _declaredQueues.Add(name);
            }
        }
    }
}
