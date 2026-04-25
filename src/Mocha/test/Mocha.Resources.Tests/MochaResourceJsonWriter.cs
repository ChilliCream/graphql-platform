using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Mocha.Resources.Tests;

/// <summary>
/// Test helper that serialises a single <see cref="MochaResource"/> to a stable JSON string,
/// mirroring the wire shape the cloud-bugfix consumer uses (kind/id/attributes object).
/// </summary>
internal static class MochaResourceJsonWriter
{
    public static string Serialize(MochaResource resource)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("kind", resource.Kind);
            writer.WriteString("id", resource.Id);
            writer.WriteStartObject("attributes");
            resource.Write(writer);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
