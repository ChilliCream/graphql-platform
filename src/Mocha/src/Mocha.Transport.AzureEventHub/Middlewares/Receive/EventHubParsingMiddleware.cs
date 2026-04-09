using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.AzureEventHub.Features;

namespace Mocha.Transport.AzureEventHub.Middlewares;

/// <summary>
/// Receive middleware that parses the raw Event Hub <see cref="Azure.Messaging.EventHubs.EventData"/>
/// into a <see cref="MessageEnvelope"/> and sets it on the receive context for downstream processing.
/// </summary>
internal sealed class EventHubParsingMiddleware
{
    /// <summary>
    /// Parses the Event Hub event data into a message envelope and invokes the next middleware.
    /// </summary>
    /// <param name="context">The receive context containing the current message features.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<EventHubReceiveFeature>();
        var eventData = feature.EventData;

        var envelope = EventHubMessageEnvelopeParser.Instance.Parse(eventData);

        context.SetEnvelope(envelope);

        await next(context);
    }

    private static readonly EventHubParsingMiddleware s_instance = new();

    /// <summary>
    /// Creates a <see cref="ReceiveMiddlewareConfiguration"/> that wraps the parsing middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "EventHubParsing".</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(static (_, next) => ctx => s_instance.InvokeAsync(ctx, next), "EventHubParsing");
}
