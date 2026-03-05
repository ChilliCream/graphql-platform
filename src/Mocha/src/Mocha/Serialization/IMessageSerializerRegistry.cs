namespace Mocha;

/// <summary>
/// Provides a registry that resolves <see cref="IMessageSerializer"/> instances by content type and message CLR type.
/// </summary>
public interface IMessageSerializerRegistry
{
    /// <summary>
    /// Gets a serializer for the specified content type and message type combination, or <c>null</c> if none is registered.
    /// </summary>
    /// <param name="contentType">The content type to look up.</param>
    /// <param name="type">The CLR type of the message.</param>
    /// <returns>A serializer, or <c>null</c> if no matching serializer is found.</returns>
    IMessageSerializer? GetSerializer(MessageContentType contentType, Type type);
}
