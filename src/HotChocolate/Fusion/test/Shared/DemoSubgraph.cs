using System.Text.Json;
using HotChocolate.Fusion.Composition;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Microsoft.AspNetCore.TestHost;

namespace HotChocolate.Fusion.Shared;

public sealed class DemoSubgraph
{
    private static readonly SyntaxSerializerOptions _serializerOptions =
        new()
        {
            Indented = true,
            MaxDirectivesPerLine = 0
        };

    public DemoSubgraph(
        string name,
        Uri httpEndpointUri,
        Uri webSocketEndpointUri,
        DocumentNode schema,
        TestServer server)
    {
        Name = name;
        HttpEndpointUri = httpEndpointUri;
        WebSocketEndpointUri = webSocketEndpointUri;
        Schema = schema;
        Server = server;
    }

    public string Name { get; }

    public Uri HttpEndpointUri { get; }

    public Uri WebSocketEndpointUri { get; }

    public DocumentNode Schema { get; }

    public TestServer Server { get; }

    public SubgraphConfiguration ToConfiguration(
        string extensions,
        bool onlyHttp = false)
        => onlyHttp
            ? new SubgraphConfiguration(
                Name,
                Schema.ToString(_serializerOptions),
                Utf8GraphQLParser.Parse(extensions).ToString(_serializerOptions),
                new IClientConfiguration[] { new HttpClientConfiguration(HttpEndpointUri), },
                null)
            : new SubgraphConfiguration(
                Name,
                Schema.ToString(_serializerOptions),
                Utf8GraphQLParser.Parse(extensions).ToString(_serializerOptions),
                new IClientConfiguration[]
                {
                    new HttpClientConfiguration(HttpEndpointUri),
                    new WebSocketClientConfiguration(WebSocketEndpointUri)
                },
                null);

    public SubgraphConfiguration ToConfiguration(
        string extensions,
        JsonElement configurationExtensions,
        bool onlyHttp = false)
        => onlyHttp
            ? new SubgraphConfiguration(
                Name,
                Schema.ToString(_serializerOptions),
                Utf8GraphQLParser.Parse(extensions).ToString(_serializerOptions),
                new IClientConfiguration[] { new HttpClientConfiguration(HttpEndpointUri), },
                configurationExtensions)
            : new SubgraphConfiguration(
                Name,
                Schema.ToString(_serializerOptions),
                Utf8GraphQLParser.Parse(extensions).ToString(_serializerOptions),
                new IClientConfiguration[]
                {
                    new HttpClientConfiguration(HttpEndpointUri),
                    new WebSocketClientConfiguration(WebSocketEndpointUri)
                },
                configurationExtensions);

    public SubgraphConfiguration ToConfiguration(bool onlyHttp = false)
        => onlyHttp
            ? new SubgraphConfiguration(
                Name,
                Schema.ToString(_serializerOptions),
                Array.Empty<string>(),
                new IClientConfiguration[] { new HttpClientConfiguration(HttpEndpointUri), },
                null)
            : new SubgraphConfiguration(
                Name,
                Schema.ToString(_serializerOptions),
                Array.Empty<string>(),
                new IClientConfiguration[]
                {
                    new HttpClientConfiguration(HttpEndpointUri),
                    new WebSocketClientConfiguration(WebSocketEndpointUri)
                },
                null);
}
