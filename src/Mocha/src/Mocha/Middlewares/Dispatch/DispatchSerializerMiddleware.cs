using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Ensures outgoing messages are serialized into an envelope before transport dispatch.
/// </summary>
/// <remarks>
/// Serialization only runs when the envelope is not already pre-built, which allows advanced
/// callers to provide a custom envelope/body path.
/// Without this middleware, transports can receive incomplete dispatch contexts and fail at runtime
/// with missing body, content-type, or envelope metadata.
/// </remarks>
internal sealed class DispatchSerializerMiddleware()
{
    public async ValueTask InvokeAsync(IDispatchContext context, DispatchDelegate next)
    {
        // If the body is empty, we need to serialize the message
        if (context.Envelope is null)
        {
            if (context.Message is null)
            {
                throw new InvalidOperationException(
                    "To send a message either the body must be set or the message must be set");
            }

            if (context.MessageType is null)
            {
                throw new InvalidOperationException(
                    "To send a message a message type must be set. Otherwise there is no way to serialize the message");
            }

            if (context.ContentType is null)
            {
                throw new InvalidOperationException(
                    "To send a message a content type must be set. Otherwise there is no way to serialize the message");
            }

            var serializer = context.MessageType.GetSerializer(context.ContentType);

            if (serializer is null)
            {
                throw new InvalidOperationException(
                    $"No serializer found for content type {context.ContentType.Value} and message type {context.MessageType.Identity}");
            }

            serializer.Serialize(context.Message, context.Body);

            // Envelope is materialized after serialization so body metadata reflects final bytes.
            context.Envelope = context.CreateEnvelope();
        }

        await next(context);
    }

    public static DispatchMiddlewareConfiguration Create()
        => new(
            static (_, next) =>
            {
                var middleware = new DispatchSerializerMiddleware();
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Serialization");
}

/// <summary>
/// Extension methods for <see cref="IDispatchContext"/> used during dispatch serialization.
/// </summary>
public static class DispatchContextExtensions
{
    /// <summary>
    /// Creates a <see cref="MessageEnvelope"/> from the dispatch context, copying message metadata and headers.
    /// </summary>
    /// <param name="context">The dispatch context to create the envelope from.</param>
    /// <returns>A new message envelope populated from the context.</returns>
    public static MessageEnvelope CreateEnvelope(this IDispatchContext context)
    {
        return new MessageEnvelope
        {
            MessageId = context.MessageId,
            CorrelationId = context.CorrelationId,
            ConversationId = context.ConversationId,
            CausationId = context.CausationId,
            SourceAddress = context.SourceAddress?.ToString(),
            DestinationAddress = context.DestinationAddress?.ToString(),
            ResponseAddress = context.ResponseAddress?.ToString(),
            FaultAddress = context.FaultAddress?.ToString(),
            ContentType = context.ContentType?.Value,
            MessageType = context.MessageType?.Identity,
            EnclosedMessageTypes = context.MessageType?.EnclosedMessageIdentities,
            Host = context.Host,
            SentAt = context.SentAt,
            DeliverBy = context.DeliverBy,
            ScheduledTime = context.ScheduledTime,
            DeliveryCount = 0,
            Headers = context.Headers,
            Body = context.Body.WrittenMemory
        };
    }
}
