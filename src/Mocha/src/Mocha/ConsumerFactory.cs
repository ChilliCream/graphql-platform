using System.ComponentModel;

namespace Mocha;

/// <summary>
/// Provides factory methods for creating consumer instances.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConsumerFactory
{
    public static Consumer Subscribe<THandler, TEvent>()
        where THandler : class, IEventHandler<TEvent>
        => new SubscribeConsumer<THandler, TEvent>();

    public static Consumer Send<THandler, TRequest>()
        where THandler : class, IEventRequestHandler<TRequest>
        where TRequest : notnull
        => new SendConsumer<THandler, TRequest>();

    public static Consumer Request<THandler, TRequest, TResponse>()
        where THandler : class, IEventRequestHandler<TRequest, TResponse>
        where TRequest : IEventRequest<TResponse>
        => new RequestConsumer<THandler, TRequest, TResponse>();

    public static Consumer Consume<TConsumer, TMessage>()
        where TConsumer : class, IConsumer<TMessage>
        => new ConsumerAdapter<TConsumer, TMessage>();

    public static Consumer Batch<THandler, TEvent>()
        where THandler : class, IBatchEventHandler<TEvent>
        => new BatchConsumer<THandler, TEvent>();
}
