using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;

namespace HotChocolate.Fusion.Packaging.Serializers;

internal static class ArchiveMetadataSerializer
{
    public static void Format(ArchiveMetadata archiveMetadata, IBufferWriter<byte> writer)
    {
        using var jsonWriter = new Utf8JsonWriter(writer);

        jsonWriter.WriteStartObject();

        jsonWriter.WriteString("formatVersion", archiveMetadata.FormatVersion.ToString());

        jsonWriter.WriteStartArray("supportedGatewayFormats");
        foreach (var format in archiveMetadata.SupportedGatewayFormats)
        {
            jsonWriter.WriteStringValue(format.ToString());
        }

        jsonWriter.WriteEndArray();

        jsonWriter.WriteStartArray("sourceSchemas");
        foreach (var schema in archiveMetadata.SourceSchemas)
        {
            jsonWriter.WriteStringValue(schema);
        }

        jsonWriter.WriteEndArray();

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();
    }

    public static ArchiveMetadata Parse(ReadOnlyMemory<byte> data)
    {
        var document = JsonDocument.Parse(data);
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

        var supportedGatewayFormatsProp = root.GetProperty("supportedGatewayFormats");
        if (supportedGatewayFormatsProp.ValueKind is not JsonValueKind.Array)
        {
            throw new JsonException("The archive metadata must contain a supportedGatewayFormats property.");
        }

        var sourceSchemasProp = root.GetProperty("sourceSchemas");
        if (sourceSchemasProp.ValueKind is not JsonValueKind.Array)
        {
            throw new JsonException("The archive metadata must contain a sourceSchemas property.");
        }

        var supportedGatewayFormats = ImmutableArray.CreateBuilder<Version>();
        foreach (var format in supportedGatewayFormatsProp.EnumerateArray())
        {
            if (format.ValueKind is not JsonValueKind.String)
            {
                throw new JsonException("The supportedGatewayFormats property must contain only strings.");
            }

            supportedGatewayFormats.Add(new Version(format.GetString()!));
        }

        var sourceSchemas = ImmutableArray.CreateBuilder<string>();
        foreach (var schema in sourceSchemasProp.EnumerateArray())
        {
            if (schema.ValueKind is not JsonValueKind.String)
            {
                throw new JsonException("The sourceSchemas property must contain only strings.");
            }

            sourceSchemas.Add(schema.GetString()!);
        }

        return new ArchiveMetadata
        {
            FormatVersion = new Version(formatVersionProp.GetString()!),
            SupportedGatewayFormats = supportedGatewayFormats.ToImmutable(),
            SourceSchemas = sourceSchemas.ToImmutable()
        };
    }
}
