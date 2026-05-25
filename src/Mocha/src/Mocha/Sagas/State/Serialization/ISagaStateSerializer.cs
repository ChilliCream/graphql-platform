using System.Buffers;

namespace Mocha.Sagas;

/// <summary>
/// Provides serialization and deserialization of saga state objects for persistence.
/// </summary>
public interface ISagaStateSerializer
{
    /// <summary>
    /// Deserializes a saga state from binary data to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="body">The binary data containing the serialized state.</param>
    /// <returns>The deserialized state, or <c>null</c> if deserialization fails.</returns>
    T? Deserialize<T>(ReadOnlyMemory<byte> body);

    /// <summary>
    /// Deserializes a saga state from binary data to an untyped object.
    /// </summary>
    /// <param name="body">The binary data containing the serialized state.</param>
    /// <returns>The deserialized state, or <c>null</c> if deserialization fails.</returns>
    object? Deserialize(ReadOnlyMemory<byte> body);

    /// <summary>
    /// Serializes a saga state to the specified buffer writer.
    /// </summary>
    /// <typeparam name="T">The type of the state to serialize.</typeparam>
    /// <param name="message">The state object to serialize.</param>
    /// <param name="writer">The buffer writer to write the serialized data to.</param>
    void Serialize<T>(T message, IBufferWriter<byte> writer);

    /// <summary>
    /// Serializes a saga state object to the specified buffer writer.
    /// </summary>
    /// <param name="message">The state object to serialize.</param>
    /// <param name="writer">The buffer writer to write the serialized data to.</param>
    void Serialize(object message, IBufferWriter<byte> writer);
}
