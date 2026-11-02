using Microsoft.Extensions.DependencyInjection;

namespace Mocha;

/// <summary>
/// Consumer adapter for <see cref="IConsumer{TMessage}"/> implementations.
/// </summary>
/// <remarks>
/// Like <see cref="SubscribeConsumer{THandler, TEvent}"/>, this wraps a user-facing consumer
/// as a bus-internal <see cref="Consumer"/>, but passes the full <see cref="IConsumeContext{TMessage}"/>
/// instead of just the deserialized message.
/// </remarks>
internal sealed class ConsumerAdapter<TConsumer, TMessage> : Consumer
    where TConsumer : class, IConsumer<TMessage>
{
    public ConsumerAdapter() : base(typeof(TConsumer)) { }

    protected override void Configure(IConsumerDescriptor descriptor)
    {
        descriptor
            .Name(typeof(TConsumer).Name)
            .AddRoute(r => r.MessageType(typeof(TMessage)).Kind(InboundRouteKind.Subscribe));
    }

    protected override async ValueTask ConsumeAsync(IConsumeContext context)
    {
        var consumer = context.Services.GetRequiredService<TConsumer>();
        var typedContext = new ConsumeContext<TMessage>(context);
        await consumer.ConsumeAsync(typedContext);
    }
}
