using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Utils;

namespace Mocha;

/// <summary>
/// Provides extension methods on <see cref="IMessageContext"/> for deserializing and caching the message payload.
/// </summary>
public static class MessageFeatureContextExtensions
{
    /// <summary>
    /// Deserializes the message payload from the envelope as the specified message type, caching the result for subsequent calls.
    /// </summary>
    /// <typeparam name="TMessage">The expected message type to deserialize.</typeparam>
    /// <param name="context">The message context containing the envelope and serialization metadata.</param>
    /// <returns>The deserialized message, or <c>null</c> if the body is empty.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the envelope, message type, content type, or a matching serializer is not available.
    /// </exception>
    public static TMessage? GetMessage<TMessage>(this IMessageContext context)
    {
        var feature = context.Features.GetOrSet<MessageParsingFeature>();
        if (feature.Message is TMessage messageOfT)
        {
            return messageOfT;
        }

        if (context.Envelope is null)
        {
            throw new InvalidOperationException("Envelope is required for deserialization");
        }

        var serializer = context.GetSerializer();

        var message = serializer.Deserialize<TMessage>(context.Envelope.Body);

        feature.Message = message;

        return message;
    }

    /// <summary>
    /// Deserializes the message payload from the envelope as an untyped object, caching the result for subsequent calls.
    /// </summary>
    /// <param name="context">The message context containing the envelope and serialization metadata.</param>
    /// <returns>The deserialized message object, or <c>null</c> if the body is empty.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the envelope, message type, content type, or a matching serializer is not available.
    /// </exception>
    public static object? GetMessage(this IMessageContext context)
    {
        var feature = context.Features.GetOrSet<MessageParsingFeature>();
        if (feature.Message is not null)
        {
            return feature.Message;
        }

        if (context.Envelope is null)
        {
            throw new InvalidOperationException("Envelope is required for deserialization");
        }

        var serializer = context.GetSerializer();

        var message = serializer.Deserialize(context.Envelope.Body);

        feature.Message = message;
        return message;
    }

    private static IMessageSerializer GetSerializer(this IMessageContext context)
    {
        if (context.MessageType is null)
        {
            throw new InvalidOperationException("Message type is required for deserialization");
        }

        if (context.ContentType is null)
        {
            throw new InvalidOperationException("Content type is required for deserialization");
        }

        var serializer = context.MessageType.GetSerializer(context.ContentType);

        if (serializer is null)
        {
            throw new InvalidOperationException(
                $"No serializer was found for message type {context.MessageType.Identity} and content type {context.ContentType}");
        }

        return serializer;
    }
}
