using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using CookieCrumble.Formatters;

namespace HotChocolate.Execution;

internal sealed class OperationRequestSnapshotFormatter : SnapshotValueFormatter<IOperationRequest>
{
    public static OperationRequestSnapshotFormatter Instance { get; } = new OperationRequestSnapshotFormatter();

    protected override void Format(IBufferWriter<byte> snapshot, IOperationRequest value)
    {
        var jsonOptions = new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var jsonWriter = new Utf8JsonWriter(snapshot, jsonOptions);

        switch (value)
        {
            case VariableBatchRequest vr:
                WriteVariableBatchRequest(jsonWriter, vr);
                break;

            case OperationRequest or:
                WriteOperationRequest(jsonWriter, or);
                break;

            default:
                throw new NotSupportedException();
        }

        jsonWriter.Flush();
    }

    private static void WriteOperationRequest(Utf8JsonWriter writer, OperationRequest request)
    {
        writer.WriteStartObject();

        WriteCommonProperties(writer, request);

        if (request.VariableValues is not null)
        {
            writer.WritePropertyName("variableValues");
            request.VariableValues.Document.RootElement.WriteTo(writer);
        }

        writer.WriteEndObject();
    }

    private static void WriteVariableBatchRequest(Utf8JsonWriter writer, VariableBatchRequest request)
    {
        writer.WriteStartObject();

        WriteCommonProperties(writer, request);

        writer.WritePropertyName("variableValues");
        request.VariableValues.Document.RootElement.WriteTo(writer);

        writer.WriteEndObject();
    }

    private static void WriteCommonProperties(Utf8JsonWriter writer, IOperationRequest request)
    {
        if (request.Document is not null)
        {
            writer.WriteString("document", request.Document.ToString());
        }

        if (request.DocumentId.HasValue)
        {
            writer.WriteString("documentId", request.DocumentId.Value);
        }

        if (!request.DocumentHash.IsEmpty)
        {
            writer.WriteStartObject("documentHash");
            writer.WriteString("algorithm", request.DocumentHash.AlgorithmName);
            writer.WriteString("value", request.DocumentHash.Value);
            writer.WriteEndObject();
        }

        if (request.OperationName is not null)
        {
            writer.WriteString("operationName", request.OperationName);
        }

        if (request.ErrorHandlingMode is not null)
        {
            writer.WriteString("errorHandlingMode", request.ErrorHandlingMode.Value.ToString());
        }

        if (request.Extensions is not null)
        {
            writer.WritePropertyName("extensions");
            request.Extensions.Document.RootElement.WriteTo(writer);
        }

        if (request.ContextData?.Count > 0)
        {
            writer.WriteStartObject("contextData");
            foreach (var kvp in request.ContextData)
            {
                writer.WritePropertyName(kvp.Key);
                WriteValue(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }

        if (request.Services is not null)
        {
            writer.WriteBoolean("hasServices", true);
        }

        if (request.Flags != RequestFlags.AllowAll)
        {
            writer.WriteString("flags", request.Flags.ToString());
        }
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case decimal dec:
                writer.WriteNumberValue(dec);
                break;
            default:
                writer.WriteStringValue($"[{value.GetType().Name}]");
                break;
        }
    }
}
