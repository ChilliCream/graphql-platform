using System.Buffers;
using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal static class FusionMigrationHelpers
{
    private static readonly JsonWriterOptions s_writerOptions = new()
    {
        Indented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static JsonDocument MigrateSubgraphConfig(ReadOnlyMemory<byte> sourceJson)
    {
        using var document = JsonDocument.Parse(sourceJson);
        var root = document.RootElement;

        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, s_writerOptions);

        writer.WriteStartObject();

        // Enable backwards compatibility
        writer.WriteString("version", "1.0.0");

        // "subgraph" -> "name"
        if (root.TryGetProperty("subgraph", out var subgraphElement))
        {
            writer.WritePropertyName("name");
            subgraphElement.WriteTo(writer);
        }
        else
        {
            writer.WriteString("name", "");
        }

        // "http" -> "transports.http" with "baseAddress" -> "url"
        if (root.TryGetProperty("http", out var httpElement))
        {
            writer.WriteStartObject("transports");
            writer.WriteStartObject("http");

            foreach (var httpProperty in httpElement.EnumerateObject())
            {
                if (httpProperty.NameEquals("baseAddress"))
                {
                    writer.WritePropertyName("url");
                    httpProperty.Value.WriteTo(writer);
                }
                else
                {
                    httpProperty.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        // Copy any other top-level properties except "subgraph", "http", and "websocket"
        foreach (var property in root.EnumerateObject())
        {
            if (property.NameEquals("subgraph")
                || property.NameEquals("http")
                || property.NameEquals("websocket"))
            {
                continue;
            }

            property.WriteTo(writer);
        }

        writer.WriteEndObject();
        writer.Flush();

        return JsonDocument.Parse(buffer.WrittenMemory);
    }

    public static JsonDocument MigrateGatewaySettings(ReadOnlyMemory<byte> sourceJson)
    {
        using var document = JsonDocument.Parse(sourceJson);
        var root = document.RootElement;

        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, s_writerOptions);

        writer.WriteStartObject();

        // preprocessor
        writer.WriteStartObject("preprocessor");

        // tagDirective.exclude -> preprocessor.excludeByTag
        if (root.TryGetProperty("tagDirective", out var tagDirective)
            && tagDirective.TryGetProperty("exclude", out var exclude)
            && exclude.ValueKind == JsonValueKind.Array
            && exclude.GetArrayLength() > 0)
        {
            writer.WritePropertyName("excludeByTag");
            exclude.WriteTo(writer);
        }

        writer.WriteEndObject();

        // merger
        writer.WriteStartObject("merger");

        // nodeField.enabled -> merger.enableGlobalObjectIdentification
        if (root.TryGetProperty("nodeField", out var nodeField)
            && nodeField.TryGetProperty("enabled", out var nodeFieldEnabled))
        {
            writer.WriteBoolean("enableGlobalObjectIdentification", nodeFieldEnabled.GetBoolean());
        }

        // tagDirective.makePublic -> merger.tagMergeBehavior = "Include"
        if (root.TryGetProperty("tagDirective", out var tagDir)
            && tagDir.TryGetProperty("makePublic", out var makePublic)
            && makePublic.GetBoolean())
        {
            writer.WriteString("tagMergeBehavior", "Include");
        }

        writer.WriteEndObject();

        // satisfiability (empty default)
        writer.WriteStartObject("satisfiability");
        writer.WriteEndObject();

        writer.WriteEndObject();
        writer.Flush();

        return JsonDocument.Parse(buffer.WrittenMemory);
    }
}
