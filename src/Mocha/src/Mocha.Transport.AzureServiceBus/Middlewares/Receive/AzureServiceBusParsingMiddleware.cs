using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.AzureServiceBus.Features;

namespace Mocha.Transport.AzureServiceBus.Middlewares;

/// <summary>
/// Receive middleware that parses the raw Azure Service Bus message into a <see cref="MessageEnvelope"/>
/// and sets it on the receive context for downstream processing.
/// </summary>
internal sealed class AzureServiceBusParsingMiddleware
{
    /// <summary>
    /// Parses the Azure Service Bus message into a message envelope and invokes the next middleware.
    /// </summary>
    /// <param name="context">The receive context containing the current message features.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<AzureServiceBusReceiveFeature>();
        var message = feature.ProcessMessageEventArgs.Message;

        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(message);

        context.SetEnvelope(envelope);

        await next(context);
    }

    private static readonly AzureServiceBusParsingMiddleware s_instance = new();

    /// <summary>
    /// Creates a <see cref="ReceiveMiddlewareConfiguration"/> that wraps the parsing middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "AzureServiceBusParsing".</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(static (_, next) => ctx => s_instance.InvokeAsync(ctx, next), "AzureServiceBusParsing");
}
