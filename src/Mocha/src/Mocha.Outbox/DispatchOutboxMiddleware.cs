using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;

namespace Mocha.Outbox;

/// <summary>
/// Persists outgoing publications, sends, replies, and faults in the outbox.
/// All other message kinds pass through to the next middleware.
/// </summary>
public sealed class DispatchOutboxMiddleware
{
    /// <summary>
    /// Evaluates whether the message should be persisted to the outbox or forwarded down the pipeline.
    /// </summary>
    /// <param name="context">The current dispatch context containing the message envelope and metadata.</param>
    /// <param name="next">The next middleware delegate in the dispatch pipeline.</param>
    /// <returns>A value task that completes when the message has been persisted or forwarded.</returns>
    public async ValueTask InvokeAsync(IDispatchContext context, DispatchDelegate next)
    {
        if (context is DispatchContext { MessageStore: { } store })
        {
            DispatchSerializerMiddleware.Serialize(context);
            await store.PersistAsync(context);
            return;
        }

        var messageKind = context.Headers.Get(MessageHeaders.MessageKind);
        if (messageKind is not MessageKind.Publish
            and not MessageKind.Send
            and not MessageKind.Reply
            and not MessageKind.Fault)
        {
            await next(context);
        }
        else
        {
            var outbox = context.Services.GetRequiredService<IMessageOutbox>();
            DispatchSerializerMiddleware.Serialize(context);
            await outbox.PersistAsync(context.Envelope!, context.CancellationToken);
        }
    }

    /// <summary>
    /// Creates the outbox middleware configuration.
    /// </summary>
    public static DispatchMiddlewareConfiguration Create()
        => new(
            static (_, next) =>
            {
                var middleware = new DispatchOutboxMiddleware();
                return context => middleware.InvokeAsync(context, next);
            },
            "Outbox");
}
