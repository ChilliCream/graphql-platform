using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;

namespace HotChocolate.Adapters.Mcp.Packaging.Serializers;

internal static class ArchiveMetadataSerializer
{
    public static void Format(ArchiveMetadata archiveMetadata, IBufferWriter<byte> writer)
    {
        using var jsonWriter = new Utf8JsonWriter(writer);

        jsonWriter.WriteStartObject();

        jsonWriter.WriteString("formatVersion", archiveMetadata.FormatVersion.ToString());

        if (archiveMetadata.Prompts.Length > 0)
        {
            jsonWriter.WriteStartArray("prompts");
            foreach (var prompt in archiveMetadata.Prompts)
            {
                jsonWriter.WriteStringValue(prompt);
            }
            jsonWriter.WriteEndArray();
        }

        if (archiveMetadata.Tools.Length > 0)
        {
            jsonWriter.WriteStartArray("tools");
            foreach (var tool in archiveMetadata.Tools)
            {
                jsonWriter.WriteStringValue(tool);
            }
            jsonWriter.WriteEndArray();
        }

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

        var prompts = ImmutableArray<string>.Empty;
        if (root.TryGetProperty("prompts", out var promptsProp)
            && promptsProp.ValueKind is JsonValueKind.Array)
        {
            var builder = ImmutableArray.CreateBuilder<string>();
            foreach (var item in promptsProp.EnumerateArray())
            {
                if (item.ValueKind is JsonValueKind.String)
                {
                    builder.Add(item.GetString()!);
                }
            }
            prompts = builder.ToImmutable();
        }

        var tools = ImmutableArray<string>.Empty;
        if (root.TryGetProperty("tools", out var toolsProp)
            && toolsProp.ValueKind is JsonValueKind.Array)
        {
            var builder = ImmutableArray.CreateBuilder<string>();
            foreach (var item in toolsProp.EnumerateArray())
            {
                if (item.ValueKind is JsonValueKind.String)
                {
                    builder.Add(item.GetString()!);
                }
            }
            tools = builder.ToImmutable();
        }

        return new ArchiveMetadata
        {
            FormatVersion = new Version(formatVersionProp.GetString()!),
            Prompts = prompts,
            Tools = tools
        };
    }
}
