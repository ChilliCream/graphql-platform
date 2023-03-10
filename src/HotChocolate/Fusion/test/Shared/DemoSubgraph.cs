using HotChocolate.Fusion.Composition;
using HotChocolate.Language;
using Microsoft.AspNetCore.TestHost;

namespace HotChocolate.Fusion.Shared;

public sealed class DemoSubgraph
{
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
        string extensions)
        => new SubgraphConfiguration(
            Name,
            Schema.ToString(),
            extensions,
            new IClientConfiguration[]
            {
                new HttpClientConfiguration(HttpEndpointUri),
                new WebSocketClientConfiguration(WebSocketEndpointUri)
            });
}
