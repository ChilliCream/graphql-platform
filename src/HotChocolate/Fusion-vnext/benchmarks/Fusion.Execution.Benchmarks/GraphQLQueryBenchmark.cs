using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Builder;
using FusionClient = HotChocolate.Fusion.Transport.Http.DefaultGraphQLHttpClient;
using FusionGraphQLHttpRequest = HotChocolate.Fusion.Transport.Http.GraphQLHttpRequest;
using TransportClient = HotChocolate.Transport.Http.DefaultGraphQLHttpClient;
using TransportGraphQLHttpRequest = HotChocolate.Transport.Http.GraphQLHttpRequest;
using BenchmarkDotNet.Jobs;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Fusion.Text.Json;
using System.IO;
using System.Text.Json;

namespace Fusion.Execution.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, warmupCount: 3, iterationCount: 10)]
public class GraphQLQueryBenchmark
{
    private readonly Uri _requestUri = new Uri("http://localhost:5000/graphql");
    private TestServer _server = null!;
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private FusionClient _fusionClient = null!;
    private FusionGraphQLHttpRequest _fusionRequest = null!;
    private TransportClient _transportClient = null!;
    private TransportGraphQLHttpRequest _transportRequest = null!;
    private MemoryStream _largeResponse = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        (_server, _app) = await GraphQLServerHelper.CreateTestServer();
        _client = _server.CreateClient();

        var operationRequest = new HotChocolate.Transport.OperationRequest("{ items }");
        _fusionRequest = new FusionGraphQLHttpRequest(operationRequest, _requestUri);
        _fusionClient = new FusionClient(_client);

        _transportRequest = new TransportGraphQLHttpRequest(operationRequest, _requestUri);
        _transportClient = new TransportClient(_client);

        JsonMemory.Return(JsonMemory.Rent());
        MetaDbMemory.Return(MetaDbMemory.Rent());

        _largeResponse = new MemoryStream(File.ReadAllBytes("/Users/michael/local/hc-3/src/HotChocolate/Fusion-vnext/benchmarks/Fusion.Execution.Benchmarks/result.json"), false);

    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        // Cleanup runs once after all benchmarks
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        _server.Dispose();
    }

    [Benchmark]
    public async Task<int> Send_Large_Request_With_Transport()
    {
        using var result = await _transportClient.SendAsync(_transportRequest);
        using var document = await result.ReadAsResultAsync();
        return document.Data.GetProperty("items").GetArrayLength();
    }

    [Benchmark]
    public async Task<int> Send_Large_Request_With_Fusion()
    {
        using var result = await _fusionClient.SendAsync(_fusionRequest);
        using var document = await result.ReadAsResultAsync();
        return document.Root.GetProperty("data"u8).GetProperty("items"u8).GetArrayLength();
    }
}
