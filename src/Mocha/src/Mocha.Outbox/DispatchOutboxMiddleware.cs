using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Outbox;

/// <summary>
/// Dispatch middleware that intercepts outgoing messages and persists them to the outbox store
/// instead of forwarding them to the next pipeline stage.
/// </summary>
/// <remarks>
/// Messages of kind Publish, Send, Reply, or Fault are redirected to <see cref="IMessageOutbox"/>.
/// All other message kinds (e.g., system messages) and dispatches marked with
/// <see cref="OutboxMiddlewareFeature.SkipOutbox"/> pass through to the next middleware.
/// </remarks>
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
        var feature = context.Features.GetOrSet<OutboxMiddlewareFeature>();
        var messageKind = context.Headers.Get(MessageHeaders.MessageKind);
        if (feature.SkipOutbox
            || messageKind
                is not MessageKind.Publish
                and not MessageKind.Send
                and not MessageKind.Reply
                and not MessageKind.Fault)
        {
            await next(context);
        }
        else
        {
            var outbox = context.Services.GetRequiredService<IMessageOutbox>();
            if (context.Envelope is not null)
            {
                await outbox.PersistAsync(context.Envelope, context.CancellationToken);
            }
        }
    }

    /// <summary>
    /// Creates the middleware configuration that wires the outbox middleware into the dispatch pipeline.
    /// </summary>
    /// <returns>A <see cref="DispatchMiddlewareConfiguration"/> named "Outbox" for pipeline registration.</returns>
    public static DispatchMiddlewareConfiguration Create()
        => new(
            static (_, next) =>
            {
                var middleware = new DispatchOutboxMiddleware();
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Outbox");
}
