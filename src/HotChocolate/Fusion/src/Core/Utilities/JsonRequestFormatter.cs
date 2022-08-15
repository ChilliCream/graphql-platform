using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Language;
using GraphQLRequest = HotChocolate.Fusion.Clients.GraphQLRequest;

namespace HotChocolate.Fusion.Utilities;

internal sealed class JsonRequestFormatter
{
    private readonly JsonWriterOptions _options = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.Default
    };

    public void Write(IBufferWriter<byte> bufferWriter, GraphQLRequest request)
    {
        using var jsonWriter = new Utf8JsonWriter(bufferWriter, _options);
        jsonWriter.WriteStartObject();

        WriteQuery(jsonWriter, request.Document);

        if (request.VariableValues is not null)
        {
            WriteVariableValues(jsonWriter, request.VariableValues);
        }

        if (request.Extensions is not null)
        {
            WriteExtensions(jsonWriter, request.Extensions);
        }

        jsonWriter.WriteEndObject();
    }

    private void WriteQuery(Utf8JsonWriter jsonWriter, DocumentNode documentNode)
        => jsonWriter.WriteString("query", documentNode.ToString());

    private void WriteVariableValues(Utf8JsonWriter jsonWriter, ObjectValueNode variableValues)
    {
        jsonWriter.WritePropertyName("variables");
        WriteValue(jsonWriter, variableValues);
    }

    private void WriteExtensions(Utf8JsonWriter jsonWriter, ObjectValueNode extensions)
    {
        jsonWriter.WritePropertyName("extensions");
        WriteValue(jsonWriter, extensions);
    }

    private void WriteValue(Utf8JsonWriter jsonWriter, IValueNode value)
    {
        switch (value)
        {
            case ObjectValueNode objectValueNode:
                jsonWriter.WriteStartObject();
                foreach (var field in objectValueNode.Fields)
                {
                    jsonWriter.WritePropertyName(field.Name.Value);
                    WriteValue(jsonWriter, field.Value);
                }
                jsonWriter.WriteEndObject();
                break;

            case ListValueNode listValueNode:
                jsonWriter.WriteStartArray();
                foreach (var item in listValueNode.Items)
                {
                    WriteValue(jsonWriter, item);
                }
                jsonWriter.WriteEndArray();
                break;

            case StringValueNode stringValueNode:
                jsonWriter.WriteStringValue(stringValueNode.Value);
                break;

            case FloatValueNode floatValueNode:
                jsonWriter.WriteRawValue(floatValueNode.AsSpan());
                break;

            case IntValueNode intValueNode:
                jsonWriter.WriteRawValue(intValueNode.AsSpan());
                break;

            case BooleanValueNode booleanValueNode:
                jsonWriter.WriteBooleanValue(booleanValueNode.Value);
                break;

            case EnumValueNode enumValueNode:
                jsonWriter.WriteStringValue(enumValueNode.Value);
                break;

            case NullValueNode:
                jsonWriter.WriteNullValue();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}
