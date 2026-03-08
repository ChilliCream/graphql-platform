using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Features;

namespace Mocha.Inbox;

/// <summary>
/// Consumer middleware that checks the inbox for duplicate messages
/// and skips processing if the message has already been handled by the current consumer.
/// </summary>
/// <remarks>
/// This middleware runs in the consumer pipeline, after the transaction middleware,
/// so that the inbox claim participates in the same database transaction as the
/// handler's side-effects. If the transaction rolls back, the inbox claim is also
/// rolled back, allowing the message to be redelivered and reprocessed.
/// <para/>
/// Deduplication is scoped to each consumer type, so different handlers can independently
/// process the same message. This supports fan-out scenarios where a single message is
/// routed to multiple consumers.
/// </remarks>
public sealed class ConsumeInboxMiddleware
{
    /// <summary>
    /// Atomically claims the incoming message via the inbox before processing.
    /// If the message has already been claimed by the same consumer type, processing is skipped.
    /// </summary>
    /// <remarks>
    /// Uses the claim-before-process pattern to prevent duplicate message processing under
    /// concurrent delivery. Instead of checking existence and then recording after processing
    /// (which is vulnerable to TOCTOU race conditions), this middleware atomically inserts the
    /// message ID and consumer type before processing. Only the consumer instance that
    /// successfully claims the message will execute the handler.
    /// <para/>
    /// When a database transaction is active (e.g. from the transaction middleware), the inbox
    /// claim INSERT participates in that transaction. This means the claim and the handler's
    /// business data commit or rollback atomically. If the process crashes after the claim
    /// but before the transaction commits, the claim is rolled back and the message can be
    /// redelivered safely.
    /// <para/>
    /// Resilience behavior:
    /// <list type="bullet">
    /// <item>If <see cref="IMessageInbox.TryClaimAsync"/> throws a transient error, the message is
    /// passed through to the handler to guarantee at-least-once delivery.</item>
    /// </list>
    /// </remarks>
    /// <param name="context">The current consume context containing the message envelope and metadata.</param>
    /// <param name="next">The next middleware delegate in the consumer pipeline.</param>
    /// <returns>A value task that completes when the message has been processed or skipped.</returns>
    public async ValueTask InvokeAsync(IConsumeContext context, ConsumerDelegate next)
    {
        var feature = context.Features.GetOrSet<InboxMiddlewareFeature>();

        if (feature.SkipInbox)
        {
            using var skipActivity = OpenTelemetry.Source.StartActivity(
                "Inbox Check Message",
                ActivityKind.Consumer,
                Activity.Current?.Context ?? new ActivityContext());
            skipActivity?.SetTag("messaging.message_id", context.MessageId);
            skipActivity?.SetTag("inbox.skipped", true);
            skipActivity?.SetTag("inbox.claimed", false);

            await next(context);
            return;
        }

        var messageId = context.MessageId;

        if (messageId is null)
        {
            // No message ID, cannot deduplicate - pass through.
            await next(context);
            return;
        }

        var consumerType = GetConsumerType(context);

        using var activity = OpenTelemetry.Source.StartActivity(
            "Inbox Check Message",
            ActivityKind.Consumer,
            Activity.Current?.Context ?? new ActivityContext());
        activity?.SetTag("messaging.message_id", messageId);
        activity?.SetTag("inbox.skipped", false);
        activity?.SetTag("inbox.consumer_type", consumerType);

        var inbox = context.Services.GetRequiredService<IMessageInbox>();

        if (context.Envelope is null)
        {
            // No envelope available to claim. Fall back to existence check to at
            // least skip known duplicates, then process without recording.
            try
            {
                if (await inbox.ExistsAsync(messageId, consumerType, context.CancellationToken))
                {
                    activity?.SetTag("inbox.claimed", false);
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var logger = context.Services.GetService<ILogger<ConsumeInboxMiddleware>>();
                logger?.InboxExistsCheckFailed(messageId, ex);
            }

            activity?.SetTag("inbox.claimed", true);
            await next(context);
            return;
        }

        // Claim-before-process: atomically insert the message ID and consumer type into the inbox.
        // Only the consumer instance that successfully claims the message will process it.
        try
        {
            if (!await inbox.TryClaimAsync(context.Envelope, consumerType, context.CancellationToken))
            {
                // This consumer type already claimed this message, skip.
                activity?.SetTag("inbox.claimed", false);
                return;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // The claim failed (e.g. transient DB error). We pass through to the handler
            // rather than dropping the message, preferring at-least-once over at-most-once delivery.
            var logger = context.Services.GetService<ILogger<ConsumeInboxMiddleware>>();
            logger?.InboxClaimFailed(messageId, ex);
        }

        activity?.SetTag("inbox.claimed", true);
        await next(context);
    }

    /// <summary>
    /// Gets the consumer type name from the current context by reading the
    /// <see cref="ReceiveConsumerFeature.CurrentConsumer"/> identity.
    /// </summary>
    /// <param name="context">The current consume context.</param>
    /// <returns>The full type name of the current consumer, or <c>"unknown"</c> if not available.</returns>
    private static string GetConsumerType(IConsumeContext context)
    {
        var consumer = context.Features.Get<ReceiveConsumerFeature>()?.CurrentConsumer;
        return consumer?.Identity?.FullName ?? "unknown";
    }

    /// <summary>
    /// Creates the middleware configuration that wires the inbox middleware into the consumer pipeline.
    /// </summary>
    /// <returns>A <see cref="ConsumerMiddlewareConfiguration"/> named "Inbox" for pipeline registration.</returns>
    public static ConsumerMiddlewareConfiguration Create()
        => new(
            static (_, next) =>
            {
                var middleware = new ConsumeInboxMiddleware();
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Inbox");
}

/// <summary>
/// Provides high-performance source-generated log methods for the inbox middleware.
/// </summary>
internal static partial class InboxMiddlewareLogs
{
    [LoggerMessage(
        1,
        LogLevel.Warning,
        "Inbox exists check failed for message {MessageId}. Message will be processed to avoid data loss.")]
    public static partial void InboxExistsCheckFailed(
        this ILogger logger, string messageId, Exception exception);

    [LoggerMessage(
        2,
        LogLevel.Warning,
        "Inbox claim failed for message {MessageId}. Message will be processed to avoid data loss.")]
    public static partial void InboxClaimFailed(
        this ILogger logger, string messageId, Exception exception);
}

// Preserve the old name as a type alias so that external code referencing
// ReceiveInboxMiddleware by name continues to compile.
// The Create() factory now returns ConsumerMiddlewareConfiguration instead of
// ReceiveMiddlewareConfiguration, which is the intentional breaking change.

/// <summary>
/// Obsolete alias for <see cref="ConsumeInboxMiddleware"/> retained for source compatibility.
/// </summary>
[System.Obsolete("Use ConsumeInboxMiddleware instead. The inbox now runs in the consumer pipeline.")]
public sealed class ReceiveInboxMiddleware
{
    /// <summary>
    /// Creates the middleware configuration that wires the inbox middleware into the consumer pipeline.
    /// </summary>
    /// <returns>A <see cref="ConsumerMiddlewareConfiguration"/> named "Inbox" for pipeline registration.</returns>
    public static ConsumerMiddlewareConfiguration Create() => ConsumeInboxMiddleware.Create();
}
