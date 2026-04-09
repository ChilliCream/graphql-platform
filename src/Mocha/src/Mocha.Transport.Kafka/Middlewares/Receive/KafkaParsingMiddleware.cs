using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.Kafka.Features;

namespace Mocha.Transport.Kafka.Middlewares;

/// <summary>
/// Receive middleware that parses the raw Kafka <see cref="Confluent.Kafka.ConsumeResult{TKey, TValue}"/>
/// into a <see cref="MessageEnvelope"/> and sets it on the receive context for downstream processing.
/// </summary>
internal sealed class KafkaParsingMiddleware
{
    /// <summary>
    /// Parses the Kafka consume result into a message envelope and invokes the next middleware.
    /// </summary>
    /// <param name="context">The receive context containing the current message features.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<KafkaReceiveFeature>();
        var consumeResult = feature.ConsumeResult;

        var envelope = KafkaMessageEnvelopeParser.Instance.Parse(consumeResult);

        context.SetEnvelope(envelope);

        await next(context);
    }

    private static readonly KafkaParsingMiddleware s_instance = new();

    /// <summary>
    /// Creates a <see cref="ReceiveMiddlewareConfiguration"/> that wraps the parsing middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "KafkaParsing".</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(static (_, next) => ctx => s_instance.InvokeAsync(ctx, next), "KafkaParsing");
}
