using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Subscriptions.RabbitMQ.Consts;
using HotChocolate.Subscriptions.RabbitMQ.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HotChocolate.Subscriptions.RabbitMQ;

public class RabbitMqEventStream<TMessage>: ISourceStream<TMessage>
{
    public RabbitMqEventStream(ISerializer messageSerializer, ActiveConsumer consumer)
    {
        _messageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));

        if (consumer is null) throw new ArgumentNullException(nameof(consumer));
        // Begin listening, for entiner lifetime of this object, messages will be recieved and written to the channel.
        // If noone is reading from the channel (by ReadEventsAsync) messages will be buffered in the channel.
        // This should be no issue as usually ReadEventsAsync is called immediately after construction of the stream.
        _unlisten = consumer.Listen(RecieveAsync);
    }

    public async IAsyncEnumerable<TMessage> ReadEventsAsync()
    {
        while (true)
        {
            await _channel.Reader.WaitToReadAsync();
            if (_channel.Reader.TryRead(out TMessage? message))
                yield return message!;
        }
    }

    async IAsyncEnumerable<object> ISourceStream.ReadEventsAsync()
    {
        await foreach (TMessage message in ReadEventsAsync())
            yield return message!;
    }

    public ValueTask DisposeAsync()
    {
        TryStop();
        return new ValueTask();
    }

    private bool _stopped;
    private Action _unlisten;
    private readonly object _lock = new();
    private readonly ISerializer _messageSerializer;
    private readonly Channel<TMessage> _channel = Channel.CreateUnbounded<TMessage>(new UnboundedChannelOptions()
    {
        // Due to sync nature of RabbitMQ.Client, concurrency cannot occure
        SingleWriter = true,
        // As method ReadEventsAsync can be potentially called multiple times, we cannot be sure only single reader will exist
        SingleReader = false
    });

    private void TryStop()
    {
        lock (_lock)
        {
            if (_stopped)
                return;
            _unlisten();
            _channel.Writer.Complete();
            _stopped = true;
        }
    }

    private async Task RecieveAsync(object sender, BasicDeliverEventArgs args)
    {
        IModel channel = (IModel)sender;

        string msg = Encoding.UTF8.GetString(args.Body.ToArray());
        if (msg == WellKnownMessages.Completed)
        {
            TryStop();
        }
        else
        {
            TMessage message = _messageSerializer.Deserialize<TMessage>(msg);
            await _channel.Writer.WriteAsync(message);
        }

        channel.BasicAck(args.DeliveryTag, false);
    }
}
