using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Mocha.Sagas;

/// <summary>
/// A JSON-based implementation of <see cref="ISagaStateSerializer"/> that uses System.Text.Json for serialization.
/// </summary>
/// <param name="typeInfo">The JSON type information for the saga state type.</param>
public sealed class JsonSagaStateSerializer(JsonTypeInfo typeInfo) : ISagaStateSerializer
{
    /// <inheritdoc />
    public object? Deserialize(ReadOnlyMemory<byte> body) => JsonSerializer.Deserialize(body.Span, typeInfo);

    /// <inheritdoc />
    public T? Deserialize<T>(ReadOnlyMemory<byte> body)
        => JsonSerializer.Deserialize(body.Span, typeInfo) is T result ? result : default;

    /// <inheritdoc />
    public void Serialize<T>(T message, IBufferWriter<byte> writer)
    {
        using var jsonWriter = new Utf8JsonWriter(writer);
        JsonSerializer.Serialize(jsonWriter, message, typeInfo);
    }

    /// <inheritdoc />
    public void Serialize(object message, IBufferWriter<byte> writer)
    {
        using var jsonWriter = new Utf8JsonWriter(writer);
        JsonSerializer.Serialize(jsonWriter, message, typeInfo);
    }
}
