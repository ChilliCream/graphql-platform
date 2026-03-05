using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Mocha;

internal sealed class JsonMessageSerializer(JsonTypeInfo typeInfo) : IMessageSerializer
{
    public MessageContentType ContentType => MessageContentType.Json;

    public object? Deserialize(ReadOnlyMemory<byte> body) => JsonSerializer.Deserialize(body.Span, typeInfo);

    public T? Deserialize<T>(ReadOnlyMemory<byte> body)
        => JsonSerializer.Deserialize(body.Span, typeInfo) is T result ? result : default(T);

    public void Serialize<T>(T message, IBufferWriter<byte> writer)
    {
        using var jsonWriter = new Utf8JsonWriter(writer);
        JsonSerializer.Serialize(jsonWriter, message, typeInfo);
    }

    public void Serialize(object message, IBufferWriter<byte> writer)
    {
        using var jsonWriter = new Utf8JsonWriter(writer);
        JsonSerializer.Serialize(jsonWriter, message, typeInfo);
    }
}
