using System.ComponentModel;

namespace Mocha;

/// <summary>
/// Provides factory methods for creating consumer instances.
/// These methods bridge internal consumer types for source-generator use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConsumerFactory
{
    public static Func<Action<IConsumerDescriptor>?, Consumer> Subscribe<THandler, TEvent>()
        where THandler : IEventHandler<TEvent>
        => static c => new SubscribeConsumer<THandler, TEvent>(c ?? (static _ => { }));

    public static Func<Action<IConsumerDescriptor>?, Consumer> Send<THandler, TRequest>()
        where THandler : IEventRequestHandler<TRequest>
        where TRequest : notnull
        => static c => new SendConsumer<THandler, TRequest>(c ?? (static _ => { }));

    public static Func<Action<IConsumerDescriptor>?, Consumer> Request<THandler, TRequest, TResponse>()
        where THandler : IEventRequestHandler<TRequest, TResponse>
        where TRequest : IEventRequest<TResponse>
        => static c => new RequestConsumer<THandler, TRequest, TResponse>(c ?? (static _ => { }));

    public static Func<Action<IConsumerDescriptor>?, Consumer> Consume<TConsumer, TMessage>()
        where TConsumer : IConsumer<TMessage>
        => static c => new ConsumerAdapter<TConsumer, TMessage>(c ?? (static _ => { }));

    public static Func<Action<IConsumerDescriptor>?, Consumer> Batch<THandler, TEvent>()
        where THandler : IBatchEventHandler<TEvent>
        => static c => new BatchConsumer<THandler, TEvent>(c ?? (static _ => { }));
}
