using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;

namespace Mocha.Outbox;

/// <summary>
/// Persists outgoing publications, sends, replies, and faults in the registered <see cref="IMessageOutbox"/>.
/// Other message kinds and dispatches marked with <see cref="OutboxMiddlewareFeature.SkipOutbox"/> pass through
/// to the next middleware.
/// </summary>
public sealed class DispatchOutboxMiddleware
{
    /// <summary>
    /// Persists the message in the registered outbox or forwards it down the pipeline.
    /// </summary>
    /// <param name="context">The current dispatch context containing the message envelope and metadata.</param>
    /// <param name="next">The next middleware delegate in the dispatch pipeline.</param>
    /// <returns>A value task that completes when the message has been persisted or forwarded.</returns>
    /// <exception cref="InvalidOperationException">A message to persist has no envelope.</exception>
    public async ValueTask InvokeAsync(IDispatchContext context, DispatchDelegate next)
    {
        if (!IsPersistedKind(context.Headers.Get(MessageHeaders.MessageKind))
            || context.Features.Get<OutboxMiddlewareFeature>() is { SkipOutbox: true })
        {
            await next(context);
            return;
        }

        if (context.Envelope is not { } envelope)
        {
            throw ThrowHelper.EnvelopeNotSet();
        }

        var outbox = context.Services.GetRequiredService<IMessageOutbox>();
        await outbox.PersistAsync(envelope, context.CancellationToken);
    }

    private static bool IsPersistedKind(string? messageKind)
        => messageKind is MessageKind.Publish or MessageKind.Send or MessageKind.Reply or MessageKind.Fault;

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
