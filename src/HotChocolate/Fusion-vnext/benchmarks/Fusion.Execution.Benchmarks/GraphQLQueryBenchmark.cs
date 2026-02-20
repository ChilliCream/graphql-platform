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
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters;
using HotChocolate.Buffers;

namespace Fusion.Execution.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, warmupCount: 3, iterationCount: 10)]
[Config(typeof(PercentilesConfig))]
public class GraphQLQueryBenchmark
{
    private readonly Uri _requestUri = new Uri("http://localhost:5000/graphql");
    private TestServer _server = null!;
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private FusionClient _fusionClient = null!;
    private FusionGraphQLHttpRequest _fusionItemsRequest = null!;
    private FusionGraphQLHttpRequest _fusionFewItemsRequest = null!;
    private TransportClient _transportClient = null!;
    private TransportGraphQLHttpRequest _transportItemsRequest = null!;
    private TransportGraphQLHttpRequest _transportFewItemsRequest = null!;

    private sealed class PercentilesConfig : ManualConfig
    {
        public PercentilesConfig()
        {
            AddColumn(StatisticColumn.P95);
            AddExporter(CsvMeasurementsExporter.Default);
            AddExporter(RPlotExporter.Default);
        }
    }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        (_server, _app) = await GraphQLServerHelper.CreateTestServer();
        _client = _server.CreateClient();

        var items = new HotChocolate.Transport.OperationRequest("{ items }");
        var fewItems = new HotChocolate.Transport.OperationRequest("{ fewItems }");


        _fusionItemsRequest = new FusionGraphQLHttpRequest(items, _requestUri);
        _fusionFewItemsRequest = new FusionGraphQLHttpRequest(fewItems, _requestUri);
        _fusionClient = new FusionClient(_client);

        _transportItemsRequest = new TransportGraphQLHttpRequest(items, _requestUri);
        _transportFewItemsRequest = new TransportGraphQLHttpRequest(fewItems, _requestUri);
        _transportClient = new TransportClient(_client);

        JsonMemory.Reconfigure(
            static () => new FixedSizeArrayPool(
                FixedSizeArrayPoolKinds.JsonMemory,
                JsonMemory.BufferSize,
                128,
                preAllocate: true));
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
        using var result = await _transportClient.SendAsync(_transportItemsRequest);
        using var document = await result.ReadAsResultAsync();
        return document.Data.GetProperty("items"u8).GetArrayLength();
    }

    [Benchmark]
    public async Task<int> Send_Large_Request_With_Fusion()
    {
        using var result = await _fusionClient.SendAsync(_fusionItemsRequest);
        using var document = await result.ReadAsResultAsync();
        return document.Root.GetProperty("data"u8).GetProperty("items"u8).GetArrayLength();
    }


    [Benchmark]
    public async Task<int> Send_Small_Request_With_Transport()
    {
        using var result = await _transportClient.SendAsync(_transportFewItemsRequest);
        using var document = await result.ReadAsResultAsync();
        return document.Data.GetProperty("fewItems"u8).GetArrayLength();
    }

    [Benchmark]
    public async Task<int> Send_Small_Request_With_Fusion()
    {
        using var result = await _fusionClient.SendAsync(_fusionFewItemsRequest);
        using var document = await result.ReadAsResultAsync();
        return document.Root.GetProperty("data"u8).GetProperty("fewItems"u8).GetArrayLength();
    }
}
