using System.Buffers;
using System.Text.Json;
using static StrawberryShake.Json.JsonSerializationHelper;

namespace StrawberryShake.Json;

public class JsonOperationRequestSerializer
{
    public void Serialize(
        OperationRequest request,
        IBufferWriter<byte> bufferWriter,
        bool ignoreExtensions = false)
    {
        using var jsonWriter = new Utf8JsonWriter(bufferWriter);
        Serialize(request, jsonWriter, ignoreExtensions);
    }

    public void Serialize(
        OperationRequest request,
        Utf8JsonWriter jsonWriter,
        bool ignoreExtensions = false)
    {
        jsonWriter.WriteStartObject();

        WriteRequest(request, jsonWriter);
        WriteVariables(request, jsonWriter);

        if (!ignoreExtensions)
        {
            WriteExtensions(request, jsonWriter);
        }

        jsonWriter.WriteEndObject();
    }

    private static void WriteRequest(OperationRequest request, Utf8JsonWriter writer)
    {
        if (request.Strategy == RequestStrategy.PersistedOperation)
        {
            writer.WriteString("id", request.Id);
        }
        else
        {
            writer.WriteString("query", request.Document.Body);
        }

        writer.WriteString("operationName", request.Name);
    }

    private static void WriteVariables(OperationRequest request, Utf8JsonWriter writer)
    {
        if (request.Variables.Count > 0)
        {
            writer.WritePropertyName("variables");
            WriteDictionary(request.Variables, writer);
        }
    }

    private static void WriteExtensions(OperationRequest request, Utf8JsonWriter writer)
    {
        if (request.GetExtensionsOrNull() is { Count: > 0, } extensions)
        {
            writer.WritePropertyName("extensions");
            WriteDictionary(extensions, writer);
        }
    }

    public static readonly JsonOperationRequestSerializer Default = new();
}
