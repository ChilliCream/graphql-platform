using HotChocolate.Fusion.Composition;
using HotChocolate.Language;
using Microsoft.AspNetCore.TestHost;

namespace HotChocolate.Fusion.Shared;

public sealed class DemoSubgraph
{
    public DemoSubgraph(string name, Uri httpBaseAddress, DocumentNode schema, TestServer server)
    {
        Name = name;
        HttpBaseAddress = httpBaseAddress;
        Schema = schema;
        Server = server;
    }

    public string Name { get; }
    public Uri HttpBaseAddress { get; }
    public DocumentNode Schema { get; }
    public TestServer Server { get; }

    public SubgraphConfiguration ToConfiguration(
        string extensions)
        => new SubgraphConfiguration(
            Name,
            Schema.ToString(),
            extensions,
            new[] { new HttpClientConfiguration(HttpBaseAddress) });
}
