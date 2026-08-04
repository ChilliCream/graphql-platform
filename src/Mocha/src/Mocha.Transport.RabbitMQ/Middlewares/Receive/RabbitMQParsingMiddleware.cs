using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.RabbitMQ.Features;

namespace Mocha.Transport.RabbitMQ.Middlewares;

/// <summary>
/// Receive middleware that parses the raw RabbitMQ delivery into a <see cref="MessageEnvelope"/>
/// and sets it on the receive context for downstream processing.
/// </summary>
internal sealed class RabbitMQParsingMiddleware
{
    /// <summary>
    /// Parses the RabbitMQ delivery event args into a message envelope and invokes the next middleware.
    /// </summary>
    /// <param name="context">The receive context containing the current message features.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<RabbitMQReceiveFeature>();
        var eventArgs = feature.EventArgs;

        var envelope = RabbitMQMessageEnvelopeParser.Instance.Parse(eventArgs);

        context.SetEnvelope(envelope);

        await next(context);
    }

    private static readonly RabbitMQParsingMiddleware s_instance = new();

    /// <summary>
    /// Creates a <see cref="ReceiveMiddlewareConfiguration"/> that wraps the parsing middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "RabbitMQParsing".</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(static (_, next) => ctx => s_instance.InvokeAsync(ctx, next), "RabbitMQParsing");
}
