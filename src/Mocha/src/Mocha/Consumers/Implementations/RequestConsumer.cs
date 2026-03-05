using Microsoft.Extensions.DependencyInjection;

namespace Mocha;

/// <summary>
/// Consumer adapter for <see cref="IEventRequestHandler{TRequest,TResponse}"/> handlers.
/// </summary>
/// <remarks>
/// Handles request messages and emits typed responses on the caller-provided response address.
/// Without this adapter, request handlers would execute but callers would not receive correlated
/// responses.
/// </remarks>
internal sealed class RequestConsumer<THandler, TRequest, TResponse> : Consumer
    where THandler : IEventRequestHandler<TRequest, TResponse>
    where TRequest : IEventRequest<TResponse>
{
    private readonly Action<IConsumerDescriptor>? _configure;

    public RequestConsumer(Action<IConsumerDescriptor> configure)
    {
        _configure = configure;
    }

    public RequestConsumer() { }

    protected override void Configure(IConsumerDescriptor descriptor)
    {
        descriptor
            .Name(typeof(THandler).Name)
            .AddRoute(r =>
                r.MessageType(typeof(TRequest)).ResponseType(typeof(TResponse)).Kind(InboundRouteKind.Request)
            );

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

        var message = context.GetMessage<TRequest>();

        var response = await handler.HandleAsync(message!, context.CancellationToken);

        // Request contracts require a response message; null would break caller expectations.
        if (response is null)
        {
            throw new InvalidOperationException("Response is null.");
        }

        // Copy request metadata (correlation/saga-related headers) onto the reply path.
        if (context.TryCreateResponseOptions(out var options))
        {
            var dispatcher = context.Services.GetRequiredService<IMessageBus>();

            await dispatcher.ReplyAsync((object)response!, options, context.CancellationToken);
        }
    }
}
