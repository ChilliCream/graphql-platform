using System.Buffers;

namespace Mocha;

/// <summary>
/// Provides serialization and deserialization of messages to and from binary representations.
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// Gets the content type that this serializer handles (e.g., JSON, Protobuf).
    /// </summary>
    MessageContentType ContentType { get; }

    /// <summary>
    /// Deserializes a message from the binary body into the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize into.</typeparam>
    /// <param name="body">The binary message body.</param>
    /// <returns>The deserialized message, or <c>null</c> if the body is empty or represents null.</returns>
    T? Deserialize<T>(ReadOnlyMemory<byte> body);

    /// <summary>
    /// Deserializes a message from the binary body as an untyped object.
    /// </summary>
    /// <param name="body">The binary message body.</param>
    /// <returns>The deserialized message object, or <c>null</c>.</returns>
    object? Deserialize(ReadOnlyMemory<byte> body);

    /// <summary>
    /// Serializes a message of the specified type into the buffer writer.
    /// </summary>
    /// <typeparam name="T">The type of the message to serialize.</typeparam>
    /// <param name="message">The message to serialize.</param>
    /// <param name="writer">The buffer writer to write the serialized bytes to.</param>
    void Serialize<T>(T message, IBufferWriter<byte> writer);

    /// <summary>
    /// Serializes a message object into the buffer writer.
    /// </summary>
    /// <param name="message">The message to serialize.</param>
    /// <param name="writer">The buffer writer to write the serialized bytes to.</param>
    void Serialize(object message, IBufferWriter<byte> writer);
}
