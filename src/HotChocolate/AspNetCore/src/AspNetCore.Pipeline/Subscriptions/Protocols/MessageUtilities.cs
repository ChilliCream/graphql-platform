using System.Text.Json;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;
using HotChocolate.Buffers;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

internal static class MessageUtilities
{
    public static JsonWriterOptions WriterOptions { get; } =
        new() { Indented = false };

    public static JsonSerializerOptions SerializerOptions { get; } =
        new(JsonSerializerDefaults.Web);

    public static void SerializeMessage(
        PooledArrayWriter pooledArrayWriter,
        IWebSocketPayloadFormatter formatter,
        ReadOnlySpan<byte> type,
        IReadOnlyDictionary<string, object?>? payload = null,
        string? id = null)
    {
        using var jsonWriter = new Utf8JsonWriter(pooledArrayWriter, WriterOptions);
        jsonWriter.WriteStartObject();

        if (id is not null)
        {
            jsonWriter.WriteString("id", id);
        }

        jsonWriter.WriteString("type", type);

        if (payload is not null)
        {
            jsonWriter.WritePropertyName("payload");
            formatter.Format(payload, jsonWriter);
        }

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();
    }

    public static bool TryGetPayload(JsonElement root, out JsonElement payload)
    {
        if (root.TryGetProperty(Utf8MessageProperties.Payload, out var payloadValue)
            && payloadValue.ValueKind is JsonValueKind.Object)
        {
            payload = payloadValue;
            return true;
        }

        payload = default;
        return false;
    }
}
