using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;

namespace Mocha.Outbox;

/// <summary>
/// Persists outgoing publications, sends, replies, and faults in the <see cref="IMessageOutbox"/> feature of the
/// dispatch context, or in the registered <see cref="IMessageOutbox"/> when the feature is not set.
/// Other message kinds, and dispatches marked with <see cref="OutboxMiddlewareFeature.SkipOutbox"/>, pass through
/// to the next middleware.
/// </summary>
public sealed class DispatchOutboxMiddleware
{
    /// <summary>
    /// Evaluates whether the message should be persisted to the outbox or forwarded down the pipeline.
    /// </summary>
    /// <param name="context">The current dispatch context containing the message envelope and metadata.</param>
    /// <param name="next">The next middleware delegate in the dispatch pipeline.</param>
    /// <returns>A value task that completes when the message has been persisted or forwarded.</returns>
    /// <exception cref="InvalidOperationException">
    /// The dispatch is marked with <see cref="OutboxMiddlewareFeature.SkipOutbox"/> and targets a DbContext outbox,
    /// or a message to persist has no envelope.
    /// </exception>
    public async ValueTask InvokeAsync(IDispatchContext context, DispatchDelegate next)
    {
        var messageKind = context.Headers.Get(MessageHeaders.MessageKind);
        if (messageKind is not MessageKind.Publish
            and not MessageKind.Send
            and not MessageKind.Reply
            and not MessageKind.Fault)
        {
            await next(context);
            return;
        }

        var explicitOutbox = context.Features.Get<IMessageOutbox>();

        if (context.Features.Get<OutboxMiddlewareFeature>() is { SkipOutbox: true })
        {
            if (explicitOutbox is not null)
            {
                throw ThrowHelper.SkipOutboxNotSupportedForDbContextOutbox();
            }

            await next(context);
            return;
        }

        if (context.Envelope is not { } envelope)
        {
            throw ThrowHelper.EnvelopeNotSet();
        }

        var outbox = explicitOutbox ?? context.Services.GetRequiredService<IMessageOutbox>();
        await outbox.PersistAsync(envelope, context.CancellationToken);
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
