using System.Buffers;
using System.Text.Json;

namespace HotChocolate.Fusion.SourceSchema.Packaging.Serializers;

internal static class ArchiveMetadataSerializer
{
    public static void Format(ArchiveMetadata archiveMetadata, IBufferWriter<byte> writer)
    {
        using var jsonWriter = new Utf8JsonWriter(writer);

        jsonWriter.WriteStartObject();

        jsonWriter.WriteString("formatVersion", archiveMetadata.FormatVersion.ToString());

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();
    }

    public static ArchiveMetadata Parse(ReadOnlyMemory<byte> data)
    {
        using var document = JsonDocument.Parse(data);
        var root = document.RootElement;

        if (root.ValueKind is not JsonValueKind.Object)
        {
            throw new JsonException("Invalid archive metadata format.");
        }

        var formatVersionProp = root.GetProperty("formatVersion");
        if (formatVersionProp.ValueKind is not JsonValueKind.String)
        {
            throw new JsonException("The archive metadata must contain a formatVersion property.");
        }

        return new ArchiveMetadata
        {
            FormatVersion = new Version(formatVersionProp.GetString()!)
        };
    }
}
