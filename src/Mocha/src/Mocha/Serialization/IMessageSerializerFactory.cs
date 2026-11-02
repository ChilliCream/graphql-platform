namespace Mocha;

/// <summary>
/// Creates <see cref="IMessageSerializer"/> instances for a specific content type, resolving type-specific serialization metadata.
/// </summary>
public interface IMessageSerializerFactory
{
    /// <summary>
    /// Gets the content type that serializers created by this factory handle.
    /// </summary>
    MessageContentType ContentType { get; }

    /// <summary>
    /// Gets a serializer for the specified message type, or <c>null</c> if the type is not supported.
    /// </summary>
    /// <param name="type">The CLR type of the message to serialize.</param>
    /// <returns>A serializer for the type, or <c>null</c> if not supported.</returns>
    IMessageSerializer? GetSerializer(Type type);
}
