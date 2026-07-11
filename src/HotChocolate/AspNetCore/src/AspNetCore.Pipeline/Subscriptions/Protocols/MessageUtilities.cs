using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;
using HotChocolate.Buffers;
using HotChocolate.Text.Json;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

internal static class MessageUtilities
{
    public static JsonWriterOptions WriterOptions { get; } =
        new()
        {
            Indented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

    public static void SerializeMessage(
        PooledArrayWriter pooledArrayWriter,
        IWebSocketPayloadFormatter formatter,
        ReadOnlySpan<byte> type,
        IReadOnlyDictionary<string, object?>? payload = null,
        string? id = null)
    {
        var jsonWriter = new JsonWriter(pooledArrayWriter, WriterOptions);
        jsonWriter.WriteStartObject();

        if (id is not null)
        {
            jsonWriter.WritePropertyName("id"u8);
            jsonWriter.WriteStringValue(id);
        }

        jsonWriter.WritePropertyName("type"u8);
        jsonWriter.WriteStringValue(type);

        if (payload is not null)
        {
            jsonWriter.WritePropertyName("payload"u8);
            formatter.Format(payload, jsonWriter);
        }

        jsonWriter.WriteEndObject();
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
