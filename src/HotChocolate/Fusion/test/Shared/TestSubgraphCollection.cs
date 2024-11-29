using System.Net;
using HotChocolate.Execution;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Shared;

public class TestSubgraphCollection(ITestOutputHelper outputHelper, TestSubgraph[] subgraphs) : IDisposable
{
    public async Task<IRequestExecutor> GetExecutorAsync(
        FusionFeatureCollection? features = null,
        Action<FusionGatewayBuilder>? configure = null)
    {
        var fusionGraph = await GetFusionGraphAsync(features);

        return await GetExecutorAsync(fusionGraph, configure);
    }

    public async Task<Skimmed.SchemaDefinition> GetFusionGraphAsync(FusionFeatureCollection? features = null)
    {
        features ??= new FusionFeatureCollection(FusionFeatures.NodeField);

        var configurations = GetSubgraphs()
            .Select(s =>
            {
                return new SubgraphConfiguration(
                    s.SubgraphName,
                    s.Subgraph.Schema.ToString(),
                    s.Subgraph.SchemaExtensions,
                    new IClientConfiguration[]
                    {
                        new HttpClientConfiguration(new Uri("http://localhost:5000/graphql")),
                    },
                    null);
            });

        return await new FusionGraphComposer(logFactory: () => new TestCompositionLog(outputHelper))
            .ComposeAsync(configurations, features);
    }

    public void Dispose()
    {
        foreach (var subgraph in subgraphs)
        {
            subgraph.TestServer.Dispose();
        }
    }

    private async Task<IRequestExecutor> GetExecutorAsync(
        Skimmed.SchemaDefinition fusionGraph,
        Action<FusionGatewayBuilder>? configure = null)
    {
        var httpClientFactory = GetHttpClientFactory();

        var builder = new ServiceCollection()
            .AddSingleton(httpClientFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph));

        configure?.Invoke(builder);

        return await builder.BuildRequestExecutorAsync();
    }

    private IHttpClientFactory GetHttpClientFactory()
    {
        var subgraphsDictionary = GetSubgraphs()
            .ToDictionary(s => s.SubgraphName, s => s.Subgraph);

        return new TestSubgraphCollectionHttpClientFactory(subgraphsDictionary);
    }

    private IEnumerable<(string SubgraphName, TestSubgraph Subgraph)> GetSubgraphs()
        => subgraphs.Select((s, i) => ($"Subgraph_{++i}", s));

    private class TestSubgraphCollectionHttpClientFactory(Dictionary<string, TestSubgraph> subgraphs)
        : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            if (!subgraphs.TryGetValue(name, out var subgraph))
            {
                throw new ArgumentException($"No configuration found for subgraph '{name}'.");
            }

            var client = subgraph.IsOffline
                ? new HttpClient(new ErrorHandler())
                : subgraph.TestServer.CreateClient();

            client.DefaultRequestHeaders.AddGraphQLPreflight();

            return client;
        }

        private class ErrorHandler : HttpClientHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }
}
