using Microsoft.Extensions.DependencyInjection;
using Mocha.Events;

namespace Mocha;

/// <summary>
/// Consumer adapter for one-way command handlers
/// (<see cref="IEventRequestHandler{TRequest}"/>).
/// </summary>
/// <remarks>
/// Executes command logic and optionally emits an <see cref="AcknowledgedEvent"/> when the sender
/// expects a reply channel.
/// Without this acknowledgement path, send-based workflows that wait for completion can only infer
/// success via timeout behavior.
/// </remarks>
internal sealed class SendConsumer<THandler, TRequest> : Consumer
    where THandler : class, IEventRequestHandler<TRequest>
    where TRequest : notnull
{
    public SendConsumer() : base(typeof(THandler)) { }

    protected override void Configure(IConsumerDescriptor descriptor)
    {
        descriptor
            .Name(typeof(THandler).Name)
            .AddRoute(r => r.MessageType(typeof(TRequest)).Kind(InboundRouteKind.Send));
    }

    protected override async ValueTask ConsumeAsync(IConsumeContext context)
    {
        var handler = context.Services.GetRequiredService<THandler>();

        var message = context.GetMessage<TRequest>();

        await handler.HandleAsync(message!, context.CancellationToken);

        // Preserve correlation metadata and acknowledge completion when a reply path is present.
        if (context.TryCreateResponseOptions(out var options) && options.CorrelationId is not null)
        {
            var dispatcher = context.Services.GetRequiredService<IMessageBus>();

            var response = new AcknowledgedEvent(options.CorrelationId, context.MessageId);

            await dispatcher.ReplyAsync(response, options, context.CancellationToken);
        }
    }
}
