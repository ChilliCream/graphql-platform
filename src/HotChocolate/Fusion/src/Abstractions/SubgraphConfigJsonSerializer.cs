using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Composition;

namespace HotChocolate.Fusion;

/// <summary>
/// Represents the formatter for the core subgraph configuration.
/// </summary>
internal static class SubgraphConfigJsonSerializer
{
    /// <summary>
    /// Formats the subgraph configuration as JSON document.
    /// </summary>
    /// <param name="config">
    /// The subgraph configuration.
    /// </param>
    /// <returns>
    /// Returns the JSON document.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="config"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The client configuration is not supported.
    /// </exception>
    public static string Format(
        SubgraphConfigJson config)
    {
        if (config is null)
        {
            throw new ArgumentNullException(nameof(config));
        }

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

    /// <summary>
    /// Formats the subgraph configuration as JSON document.
    /// </summary>
    /// <param name="config">
    /// The subgraph configuration.
    /// </param>
    /// <param name="stream">
    /// The stream to write the JSON document to.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="config"/> is <c>null</c> or
    /// <paramref name="stream"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The client configuration is not supported.
    /// </exception>
    public static async ValueTask FormatAsync(
        SubgraphConfigJson config,
        Stream stream,
        CancellationToken cancellationToken)
    {
        if (config is null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

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

        if (config.Extensions is not null)
        {
            writer.WritePropertyName("extensions");
            config.Extensions.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Parses the subgraph configuration from a JSON document.
    /// </summary>
    /// <param name="stream">
    /// The stream to read the JSON document from.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the subgraph configuration.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// The configuration property is not supported.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The subgraph name is missing.
    /// </exception>
    public static async Task<SubgraphConfigJson> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(
            stream, cancellationToken: cancellationToken);
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

        return new SubgraphConfigJson(subgraph, configs, extensions);
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
