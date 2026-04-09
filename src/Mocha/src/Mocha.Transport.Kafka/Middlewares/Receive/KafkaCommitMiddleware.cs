using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.Kafka.Features;

namespace Mocha.Transport.Kafka.Middlewares;

/// <summary>
/// Receive middleware that commits the Kafka offset after successful processing,
/// ensuring at-least-once delivery semantics.
/// </summary>
internal sealed class KafkaCommitMiddleware
{
    /// <summary>
    /// Invokes the next middleware in the pipeline and commits the offset on success.
    /// On failure, the offset is not committed and the message will be redelivered.
    /// </summary>
    /// <param name="context">The receive context containing the current message and features.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<KafkaReceiveFeature>();

        try
        {
            await next(context);

            // Commit offset after successful processing (or successful error routing).
            // This is a synchronous call into librdkafka. Safe because the pipeline
            // runs on the consume loop thread (sequential processing).
            feature.Consumer.Commit(feature.ConsumeResult);
        }
        catch
        {
            // Do NOT commit -- message will be redelivered.
            // In practice, the fault handling middleware upstream catches most exceptions
            // and routes to the error topic, so this catch handles only catastrophic
            // failures in the error routing itself.
            throw;
        }
    }

    private static readonly KafkaCommitMiddleware s_instance = new();

    /// <summary>
    /// Creates a <see cref="ReceiveMiddlewareConfiguration"/> that wraps the commit middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "KafkaCommit".</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) => ctx => s_instance.InvokeAsync(ctx, next),
            "KafkaCommit");
}
