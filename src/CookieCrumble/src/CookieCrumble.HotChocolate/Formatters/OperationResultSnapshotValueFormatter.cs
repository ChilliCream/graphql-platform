using System.Buffers;
using System.Text.Json;
using CookieCrumble.Formatters;
using HotChocolate.Transport;

namespace CookieCrumble.HotChocolate.Formatters;

internal sealed class OperationResultSnapshotValueFormatter : SnapshotValueFormatter<OperationResult>
{
    protected override void Format(IBufferWriter<byte> snapshot, OperationResult value)
    {
        var writer = new Utf8JsonWriter(
            snapshot,
            new JsonWriterOptions
            {
                Indented = true,
                SkipValidation = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

        writer.WriteStartObject();

        if (value.RequestIndex.HasValue)
        {
            writer.WriteNumber("requestIndex", value.RequestIndex.Value);
        }

        if (value.VariableIndex.HasValue)
        {
            writer.WriteNumber("variableIndex", value.VariableIndex.Value);
        }

        if (value.Data.ValueKind is JsonValueKind.Object)
        {
            writer.WritePropertyName("data");
            value.Data.WriteTo(writer);
        }

        if (value.Errors.ValueKind is JsonValueKind.Array)
        {
            writer.WritePropertyName("errors");
            value.Errors.WriteTo(writer);
        }

        if (value.Extensions.ValueKind is JsonValueKind.Object)
        {
            writer.WritePropertyName("extensions");
            value.Extensions.WriteTo(writer);
        }

        writer.WriteEndObject();
        writer.Flush();
    }
}
