using System.Buffers;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Fusion.Composition;

internal static class PackageHelper
{
    public static async Task<SubgraphConfigurationDto> LoadSubgraphConfigAsync(
        string filename,
        CancellationToken ct)
    {
        await using var stream = File.OpenRead(filename);
        return await ParseSubgraphConfigAsync(stream, ct);
    }

    private static async Task<SubgraphConfigurationDto> ParseSubgraphConfigAsync(
        Stream stream,
        CancellationToken ct)
    {
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var configs = new List<IClientConfiguration>();
        var subgraph = default(string?);
        var extensions = default(JsonElement?);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            switch (property.Name)
            {
                case "subgraph":
                    subgraph = property.Value.GetString();
                    break;

                case "http":
                    configs.Add(ReadHttpClientConfiguration(property.Value));
                    break;

                case "websocket":
                    configs.Add(ReadWebSocketClientConfiguration(property.Value));
                    break;

                case "extensions":
                    extensions = property.Value.SafeClone();
                    break;

                default:
                    throw new NotSupportedException(
                        $"Configuration property `{property.Value}` is not supported.");
            }
        }

        if (string.IsNullOrEmpty(subgraph))
        {
            throw new InvalidOperationException("No subgraph name was specified.");
        }

        return new SubgraphConfigurationDto(subgraph, configs, extensions);
    }

    private static HttpClientConfiguration ReadHttpClientConfiguration(
        JsonElement element)
    {
        var baseAddress = new Uri(element.GetProperty("baseAddress").GetString()!);
        var clientName = default(string?);

        if (element.TryGetProperty("clientName", out var clientNameElement))
        {
            clientName = clientNameElement.GetString();
        }

        return new HttpClientConfiguration(baseAddress, clientName);
    }

    private static WebSocketClientConfiguration ReadWebSocketClientConfiguration(
        JsonElement element)
    {
        var baseAddress = new Uri(element.GetProperty("baseAddress").GetString()!);
        var clientName = default(string?);

        if (element.TryGetProperty("clientName", out var clientNameElement))
        {
            clientName = clientNameElement.GetString();
        }

        return new WebSocketClientConfiguration(baseAddress, clientName);
    }

    private static JsonElement SafeClone(this JsonElement element)
    {
        var writer = new ArrayBufferWriter<byte>();
        using var jsonWriter = new Utf8JsonWriter(writer);

        element.WriteTo(jsonWriter);
        jsonWriter.Flush();
        var reader = new Utf8JsonReader(writer.WrittenSpan, true, default);

        return JsonElement.ParseValue(ref reader);
    }

    public static string FormatSubgraphConfig(
        SubgraphConfigurationDto subgraphConfig)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        writer.WriteString("subgraph", subgraphConfig.Name);

        foreach (var client in subgraphConfig.Clients)
        {
            switch (client)
            {
                case HttpClientConfiguration config:
                    writer.WriteStartObject("http");
                    writer.WriteString("baseAddress", config.BaseAddress.ToString());

                    if (config.ClientName is not null)
                    {
                        writer.WriteString("clientName", config.ClientName);
                    }

                    writer.WriteEndObject();
                    break;

                case WebSocketClientConfiguration config:
                    writer.WriteStartObject("websocket");
                    writer.WriteString("baseAddress", config.BaseAddress.ToString());

                    if (config.ClientName is not null)
                    {
                        writer.WriteString("clientName", config.ClientName);
                    }

                    writer.WriteEndObject();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(client));
            }
        }

        if (subgraphConfig.Extensions is not null)
        {
            writer.WritePropertyName("extensions");
            subgraphConfig.Extensions.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
