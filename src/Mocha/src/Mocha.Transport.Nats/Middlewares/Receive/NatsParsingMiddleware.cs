using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.Nats.Features;

namespace Mocha.Transport.Nats.Middlewares;

/// <summary>
/// Turns the raw received message into a <see cref="MessageEnvelope"/> for the receive pipeline.
/// </summary>
internal sealed class NatsParsingMiddleware
{
    private static readonly NatsParsingMiddleware s_instance = new();

    /// <summary>
    /// Parses the message and passes it to the rest of the pipeline.
    /// </summary>
    /// <param name="context">The receive context.</param>
    /// <param name="next">The next middleware.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<NatsReceiveFeature>();

        var envelope = NatsMessageEnvelopeParser.Instance.Parse(
            feature.Headers,
            feature.Body,
            feature.DeliveryCount);

        context.SetEnvelope(envelope);

        await next(context);
    }

    /// <summary>
    /// Creates the middleware configuration.
    /// </summary>
    /// <returns>The configuration.</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(static (_, next) => ctx => s_instance.InvokeAsync(ctx, next), "NatsParsing");
}
