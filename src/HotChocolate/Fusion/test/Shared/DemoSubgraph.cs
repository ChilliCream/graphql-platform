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
            MaxDirectivesPerLine = 0,
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
        string? extensions = null,
        JsonElement? configurationExtensions = null,
        bool onlyHttp = false)
    {
        IClientConfiguration[] configs;
        {
            int configCount = 1;
            if (!onlyHttp)
            {
                configCount++;
            }
            configs = new IClientConfiguration[configCount];
            configs[0] = new HttpClientConfiguration(HttpEndpointUri);
            if (!onlyHttp)
            {
                configs[1] = new WebSocketClientConfiguration(WebSocketEndpointUri);
            }
        }

        var extensionsList = extensions is null
            ? Array.Empty<string>()
            : [ Utf8GraphQLParser.Parse(extensions).ToString(_serializerOptions) ];

        return new SubgraphConfiguration(
            Name,
            Schema.ToString(_serializerOptions),
            extensionsList,
            configs,
            configurationExtensions);
    }
}
