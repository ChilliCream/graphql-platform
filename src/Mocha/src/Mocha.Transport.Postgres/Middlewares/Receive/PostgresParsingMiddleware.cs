using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.Postgres.Features;

namespace Mocha.Transport.Postgres.Middlewares;

/// <summary>
/// Receive middleware that parses the raw PostgreSQL message item into a <see cref="MessageEnvelope"/>
/// and sets it on the receive context for downstream processing.
/// </summary>
internal sealed class PostgresParsingMiddleware
{
    /// <summary>
    /// Parses the PostgreSQL message item into a message envelope and invokes the next middleware.
    /// </summary>
    /// <param name="context">The receive context containing the current message features.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<PostgresReceiveFeature>();
        var messageItem = feature.MessageItem;

        var envelope = PostgresMessageEnvelopeParser.Instance.Parse(messageItem);

        context.SetEnvelope(envelope);

        await next(context);
    }

    private static readonly PostgresParsingMiddleware s_instance = new();

    /// <summary>
    /// Creates a <see cref="ReceiveMiddlewareConfiguration"/> that wraps the parsing middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "PostgresParsing".</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(static (_, next) => ctx => s_instance.InvokeAsync(ctx, next), "PostgresParsing");
}
