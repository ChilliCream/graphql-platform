using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Composition;

namespace HotChocolate.Fusion;

internal static class SubgraphConfigJsonSerializer
{
    public static string Format(
        SubgraphConfigJson config)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        writer.WriteString("subgraph", config.Name);

        foreach (var client in config.Clients)
        {
            switch (client)
            {
                case HttpClientConfiguration http:
                    writer.WriteStartObject("http");
                    writer.WriteString("url", http.BaseAddress.ToString());

                    if (http.ClientName is not null)
                    {
                        writer.WriteString("clientName", http.ClientName);
                    }
                    writer.WriteEndObject();
                    break;

                case WebSocketClientConfiguration websocket:
                    writer.WriteStartObject("websocket");
                    writer.WriteString("url", websocket.BaseAddress.ToString());

                    if (websocket.ClientName is not null)
                    {
                        writer.WriteString("clientName", websocket.ClientName);
                    }
                    writer.WriteEndObject();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(client));
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static async ValueTask FormatAsync(
        SubgraphConfigJson config,
        Stream stream,
        CancellationToken cancellationToken)
    {
        await using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteString("subgraph", config.Name);

        foreach (var client in config.Clients)
        {
            switch (client)
            {
                case HttpClientConfiguration http:
                    writer.WriteStartObject("http");
                    writer.WriteString("url", http.BaseAddress.ToString());

                    if (http.ClientName is not null)
                    {
                        writer.WriteString("clientName", http.ClientName);
                    }
                    writer.WriteEndObject();
                    break;

                case WebSocketClientConfiguration websocket:
                    writer.WriteStartObject("websocket");
                    writer.WriteString("url", websocket.BaseAddress.ToString());

                    if (websocket.ClientName is not null)
                    {
                        writer.WriteString("clientName", websocket.ClientName);
                    }
                    writer.WriteEndObject();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(client));
            }
        }

        writer.WriteEndObject();
        await writer.FlushAsync(cancellationToken);
    }

    public static async Task<SubgraphConfigJson> ParseAsync(
        Stream stream,
        CancellationToken ct)
    {
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var configs = new List<IClientConfiguration>();
        var subgraph = default(string?);

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

                default:
                    throw new NotSupportedException(
                        $"Configuration property `{property.Value}` is not supported.");
            }
        }

        if (string.IsNullOrEmpty(subgraph))
        {
            throw new InvalidOperationException("No subgraph name was specified.");
        }

        return new SubgraphConfigJson(subgraph, configs);
    }

    private static HttpClientConfiguration ReadHttpClientConfiguration(
        JsonElement element)
    {
        var baseAddress = new Uri(element.GetProperty("url").GetString()!);
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
        var baseAddress = new Uri(element.GetProperty("url").GetString()!);
        var clientName = default(string?);

        if (element.TryGetProperty("clientName", out var clientNameElement))
        {
            clientName = clientNameElement.GetString();
        }

        return new WebSocketClientConfiguration(baseAddress, clientName);
    }
}
