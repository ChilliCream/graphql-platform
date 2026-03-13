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
internal sealed class ConsumerAdapter<TConsumer, TMessage> : Consumer where TConsumer : IConsumer<TMessage>
{
    private readonly Action<IConsumerDescriptor>? _configure;

    public ConsumerAdapter(Action<IConsumerDescriptor> configure)
    {
        _configure = configure;
    }

    public ConsumerAdapter() { }

    protected override void Configure(IConsumerDescriptor descriptor)
    {
        descriptor
            .Name(typeof(TConsumer).Name)
            .AddRoute(r => r.MessageType(typeof(TMessage)).Kind(InboundRouteKind.Subscribe));

        _configure?.Invoke(descriptor);
    }

    protected override void OnAfterInitialize(IMessagingSetupContext context)
    {
        base.OnAfterInitialize(context);
        SetIdentity(typeof(TConsumer));
    }

    protected override async ValueTask ConsumeAsync(IConsumeContext context)
    {
        var consumer = context.Services.GetRequiredService<TConsumer>();
        var typedContext = new ConsumeContext<TMessage>(context);
        await consumer.ConsumeAsync(typedContext);
    }
}
