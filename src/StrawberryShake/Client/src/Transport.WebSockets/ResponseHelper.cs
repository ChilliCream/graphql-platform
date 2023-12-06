using System;
using System.Text.Json;
using StrawberryShake.Internal;

namespace StrawberryShake.Transport.WebSockets;

internal static class ResponseHelper
{
    internal static JsonDocument CreateBodyFromException(Exception exception)
    {
        using var bufferWriter = new ArrayWriter();

        using var jsonWriter = new Utf8JsonWriter(bufferWriter);
        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("errors");
        jsonWriter.WriteStartArray();
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("message", exception.Message);
        jsonWriter.WriteEndObject();
        jsonWriter.WriteEndArray();
        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        return JsonDocument.Parse(bufferWriter.GetWrittenMemory());
    }
}
