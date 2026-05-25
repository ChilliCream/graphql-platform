using Microsoft.Extensions.DependencyInjection;

namespace Mocha;

/// <summary>
/// Consumer adapter for event subscription handlers (<see cref="IEventHandler{TEvent}"/>).
/// </summary>
/// <remarks>
/// Represents pure publish/subscribe consumption: it handles the event and does not emit replies.
/// Keeping subscribe behavior separate from request/send consumers avoids accidental response
/// semantics on broadcast event flows.
/// </remarks>
internal sealed class SubscribeConsumer<THandler, TEvent> : Consumer where THandler : IEventHandler<TEvent>
{
    private readonly Action<IConsumerDescriptor>? _configure;

    public SubscribeConsumer(Action<IConsumerDescriptor> configure)
    {
        _configure = configure;
    }

    public SubscribeConsumer() { }

    protected override void Configure(IConsumerDescriptor descriptor)
    {
        descriptor
            .Name(typeof(THandler).Name)
            .AddRoute(r => r.MessageType(typeof(TEvent)).Kind(InboundRouteKind.Subscribe));

        _configure?.Invoke(descriptor);
    }

    protected override void OnAfterInitialize(IMessagingSetupContext context)
    {
        base.OnAfterInitialize(context);
        SetIdentity(typeof(THandler));
    }

    protected override async ValueTask ConsumeAsync(IConsumeContext context)
    {
        var handler = context.Services.GetRequiredService<THandler>();

        var message = context.GetMessage<TEvent>();

        await handler.HandleAsync(message!, context.CancellationToken);
    }
}
