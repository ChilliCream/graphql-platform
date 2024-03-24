using HotChocolate.Execution;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Shared;

public class TestSubgraphCollection(ITestOutputHelper outputHelper) : IDisposable
{
    public required TestSubgraph[] Subgraphs { get; init; }

    public IHttpClientFactory GetHttpClientFactory()
    {
        var subgraphTestServers = Subgraphs.ToDictionary(s => s.SubgraphName, s => s.TestServer);

        return new TestSubgraphCollectionHttpClientFactory(subgraphTestServers);
    }

    public async Task<Skimmed.Schema> ComposeFusionGraphAsync(FusionFeatureCollection? features = null)
    {
        features ??= new FusionFeatureCollection(FusionFeatures.NodeField);

        return await new FusionGraphComposer(logFactory: () => new TestCompositionLog(outputHelper))
            .ComposeAsync(Subgraphs.Select(subgraph =>
            {
                return new SubgraphConfiguration(
                    subgraph.SubgraphName,
                    subgraph.Schema.ToString(),
                    subgraph.SchemaExtensions,
                    new IClientConfiguration[]
                    {
                        new HttpClientConfiguration(new Uri("http://localhost:5000/graphql")),
                    },
                    null);
            }), features);
    }

    public async Task<IRequestExecutor> GetExecutor(Skimmed.Schema fusionGraph)
    {
        var httpClientFactory = GetHttpClientFactory();

        return await new ServiceCollection()
            .AddSingleton(httpClientFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();
    }

    public void Dispose()
    {
        foreach (var subgraph in Subgraphs)
        {
            subgraph.TestServer.Dispose();
        }
    }

    private class TestSubgraphCollectionHttpClientFactory(Dictionary<string, TestServer> testServers)
        : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            var client = testServers[name].CreateClient();

            client.DefaultRequestHeaders.AddGraphQLPreflight();

            return client;
        }
    }
}
