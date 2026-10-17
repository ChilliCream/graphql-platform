using HotChocolate;
using HotChocolate.Buffers;
using HotChocolate.Transport.Formatters;
using HotChocolate.Fusion;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApolloErrorVariablesMicro;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

var mode = args.Single();
if (mode is not ("verify" or "timing" or "allocation"))
{
    throw new ArgumentException("Expected verify, timing, or allocation.");
}

var cases = new Case[]
{
    new("ordinary", 1, 1, false, "success", true),
    new("unique1", 1, 1, false, "success"),
    new("unique5", 5, 5, false, "success"),
    new("unique6", 6, 6, false, "success"),
    new("unique9", 9, 9, false, "success"),
    new("duplicate96-to1", 96, 1, false, "success"),
    new("mixed96-to6", 96, 6, false, "success"),
    new("batch9", 9, 9, true, "success"),
    new("transport9", 9, 9, false, "transport"),
    new("batch-fallback9", 9, 9, true, "fallback")
};
var rows = new List<object>();
foreach (var shape in cases)
{
    var client = new FixtureClient(shape);
    var services = new ServiceCollection();
    services.AddHttpClient();
    var builder = services.AddGraphQLGateway()
        .AddInMemoryConfiguration(SchemaFixture.ComposeApolloSchema());
    builder.Services.AddSingleton<ISourceSchemaClientFactory>(new ClientFactory(client));
    FusionSetupUtilities.Configure(builder, setup =>
    {
        setup.ClientConfigurationModifiers.Add(_ => new ClientConfiguration("a"));
        setup.ClientConfigurationModifiers.Add(_ => new ClientConfiguration("b"));
    });
    var executor = await services.BuildGatewayAsync();
    var request = OperationRequestBuilder.New().SetDocument(shape.Ordinary
        ? "{ products { id } }"
        : shape.Batch
            ? "{ products { name } categories { title } }"
            : "{ products { name } }").Build();
    var initial = await executor.ExecuteAsync(request);
    using var oracleBuffer = new PooledArrayWriter();
    JsonResultFormatter.Default.Format(initial.ExpectOperationResult(), oracleBuffer);
    var oracle = Encoding.UTF8.GetString(oracleBuffer.WrittenSpan);
    await initial.DisposeAsync();
    client.Capturing = false;
    if (client.SingleCalls != (shape.Ordinary || shape.Batch ? 0 : 1)
        || client.BatchCalls != (shape.Batch ? 1 : 0)
        || client.BatchRequests != (shape.Batch ? 2 : 0))
    {
        throw new InvalidOperationException($"Unexpected dispatch for {shape.Name}.");
    }
    using (var document = JsonDocument.Parse(oracle))
    {
        var products = document.RootElement.GetProperty("data").GetProperty("products");
        if (products.GetArrayLength() != shape.Count)
        {
            throw new InvalidOperationException($"Wrong result count for {shape.Name}.");
        }
        for (var i = 0; i < shape.Count; i++)
        {
            var item = products[i];
            var id = (i % shape.Unique + 1).ToString();
            var first = item.GetProperty(shape.Ordinary ? "id" : "name");
            var expected = shape.Ordinary ? id : shape.Response is "success" ? "Name" + id : null;
            if (first.GetString() != expected
                || (shape.Batch && document.RootElement.GetProperty("data")
                    .GetProperty("categories")[i].GetProperty("title").GetString() != "Name" + id))
            {
                throw new InvalidOperationException($"Wrong response value for {shape.Name}[{i}].");
            }
        }
    }
    if (mode is "verify")
    {
        rows.Add(new { shape, oracle, client.SingleCalls, client.BatchCalls, client.BatchRequests, client.CapturedVariables });
        continue;
    }

    client.OwnerThread = Environment.CurrentManagedThreadId;
    var sync = true;
    long consumed = 0;
    void ExecuteOne()
    {
        var task = executor.ExecuteAsync(request);
        sync &= task.IsCompletedSuccessfully;
        var result = task.GetAwaiter().GetResult();
        var errors = result.ExpectOperationResult().Errors.Count;
        if ((errors != 0) != (shape.Response is not "success"))
        {
            throw new InvalidOperationException($"Wrong error state for {shape.Name}.");
        }
        consumed += errors + 1;
        var dispose = result.DisposeAsync();
        sync &= dispose.IsCompletedSuccessfully;
        dispose.GetAwaiter().GetResult();
        sync &= Environment.CurrentManagedThreadId == client.OwnerThread;
    }

    var warmStart = Stopwatch.GetTimestamp();
    var warmIterations = 0;
    do
    {
        ExecuteOne();
        warmIterations++;
    }
    while (Stopwatch.GetElapsedTime(warmStart).TotalMilliseconds < 1000);

    var blocks = new List<object>();
    if (mode is "timing")
    {
        for (var block = 0; block < 12; block++)
        {
            var iterations = shape.Response is "success" ? 5000 : 500;
            var started = DateTime.UtcNow;
            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);
            var start = Stopwatch.GetTimestamp();
            for (var i = 0; i < iterations; i++)
            {
                ExecuteOne();
            }
            var ticks = Stopwatch.GetTimestamp() - start;
            blocks.Add(new
            {
                block,
                started,
                iterations,
                ticks,
                nanosecondsPerOperation = ticks * 1e9 / Stopwatch.Frequency / iterations,
                gc = new[] { GC.CollectionCount(0) - gen0, GC.CollectionCount(1) - gen1, GC.CollectionCount(2) - gen2 }
            });
        }
    }
    else
    {
        Calibration.Allocate();
        for (var block = 0; block < 5; block++)
        {
            const int iterations = 1000;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var noGc = GC.TryStartNoGCRegion(512L * 1024 * 1024, disallowFullBlockingGC: true);
            if (!noGc)
            {
                Console.Error.WriteLine(JsonSerializer.Serialize(new
                { shape.Name, block, noGc, GCSettings.LatencyMode }));
                throw new InvalidOperationException("NoGC region unavailable.");
            }
            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);
            var calibrationBefore = Calibration.Allocate();
            var totalBefore = GC.GetTotalAllocatedBytes(precise: true);
            var threadBefore = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < iterations; i++)
            {
                ExecuteOne();
            }
            var threadBytes = GC.GetAllocatedBytesForCurrentThread() - threadBefore;
            var totalBytes = GC.GetTotalAllocatedBytes(precise: true) - totalBefore;
            var calibrationAfter = Calibration.Allocate();
            var collections = new[]
            { GC.CollectionCount(0) - gen0, GC.CollectionCount(1) - gen1, GC.CollectionCount(2) - gen2 };
            var noCollections = gen0 == GC.CollectionCount(0)
                && gen1 == GC.CollectionCount(1)
                && gen2 == GC.CollectionCount(2);
            var regionMode = GCSettings.LatencyMode;
            string? endError = null;
            try
            {
                GC.EndNoGCRegion();
            }
            catch (InvalidOperationException exception)
            {
                endError = exception.Message;
            }
            var observation = new
            {
                shape.Name, block, iterations, threadBytes, totalBytes,
                otherThreadOrCounterDifference = totalBytes - threadBytes,
                noCollections, noGc, collections, sync, client.OtherThreadObserved,
                calibrationBefore, calibrationAfter, calibrationExpected = 523240,
                regionMode, endMode = GCSettings.LatencyMode, endError
            };
            Console.Error.WriteLine(JsonSerializer.Serialize(observation));
            if (!noCollections || !sync || client.OtherThreadObserved
                || calibrationBefore != 523240 || calibrationAfter != 523240
                || regionMode != GCLatencyMode.NoGCRegion || endError is not null)
            {
                throw new InvalidOperationException("Scoped allocation validation failed.");
            }
            blocks.Add(observation);
        }
    }
    rows.Add(new
    {
        shape,
        warmIterations,
        consumed,
        sync,
        client.OtherThreadObserved,
        client.SingleCalls,
        client.BatchCalls,
        client.BatchRequests,
        oracle,
        blocks
    });
    Console.Error.WriteLine($"Completed {mode}: {shape.Name}");
}
Console.WriteLine(JsonSerializer.Serialize(new
{
    mode,
    processId = Environment.ProcessId,
    runtime = RuntimeInformation.FrameworkDescription,
    GCSettings.IsServerGC,
    GCSettings.LatencyMode,
    stopwatchFrequency = Stopwatch.Frequency,
    variableValuesSize = Unsafe.SizeOf<VariableValues>(),
    executionDllSha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(ApolloOperationExecutionNode).Assembly.Location))),
    rows
}, new JsonSerializerOptions { WriteIndented = true }));

internal sealed record Case(string Name, int Count, int Unique, bool Batch, string Response, bool Ordinary = false);

internal sealed class FixtureClient : ISourceSchemaClient
{
    private readonly Case _shape;
    private readonly byte[] _root;
    private readonly byte[] _entities;
    private readonly byte[] _categories;
    public FixtureClient(Case shape)
    {
        _shape = shape;
        var data = new Dictionary<string, object>
        {
            ["products"] = Enumerable.Range(0, shape.Count).Select(i => new
            {
                __typename = "Product", id = (i % shape.Unique + 1).ToString()
            }).ToArray()
        };
        if (shape.Batch)
        {
            data["categories"] = Enumerable.Range(0, shape.Count).Select(i => new
            {
                __typename = "Category", id = (i % shape.Unique + 1).ToString()
            }).ToArray();
        }
        _root = JsonSerializer.SerializeToUtf8Bytes(new { data });
        _entities = JsonSerializer.SerializeToUtf8Bytes(new
        {
            data = new
            {
                _entities = Enumerable.Range(1, shape.Unique).Select(i => new
                {
                    name = "Name" + i
                }).ToArray()
            }
        });
        _categories = JsonSerializer.SerializeToUtf8Bytes(new
        {
            data = new
            {
                _entities = Enumerable.Range(1, shape.Unique).Select(i => new
                {
                    title = "Name" + i
                }).ToArray()
            }
        });
    }
    public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;
    public int OwnerThread { get; set; }
    public bool Capturing { get; set; } = true;
    public List<string> CapturedVariables { get; } = [];
    public bool OtherThreadObserved { get; private set; }
    public long SingleCalls { get; private set; }
    public long BatchCalls { get; private set; }
    public long BatchRequests { get; private set; }

    public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        OtherThreadObserved |= OwnerThread != 0 && Environment.CurrentManagedThreadId != OwnerThread;
        var entity = request.Node is ApolloOperationExecutionNode;
        if (entity)
        {
            SingleCalls++;
            Capture(request);
            if (_shape.Response is "transport")
            {
                throw new InvalidOperationException("Source transport failed.");
            }
        }
        var bytes = entity ? _entities : _root;
        yield return new SourceSchemaResult(
            CompactPath.Root, SourceResultDocument.Parse(context.Memory, bytes, bytes.Length));
        await Task.CompletedTask;
    }

    public async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        OtherThreadObserved |= OwnerThread != 0 && Environment.CurrentManagedThreadId != OwnerThread;
        if (requests[0].Node is not ApolloOperationBatchExecutionNode)
        {
            throw new InvalidOperationException("Expected Apollo batch node.");
        }
        BatchCalls++;
        BatchRequests += requests.Length;
        for (var i = 0; i < requests.Length; i++)
        {
            Capture(requests[i]);
            if (requests[i].LookupTypeName is "Product" && _shape.Response is "fallback")
            {
                context.TrackBatchRequestError(requests[i].Node, i,
                    new InvalidOperationException("Source fallback failed."));
                continue;
            }
            var bytes = requests[i].LookupTypeName is "Category" ? _categories : _entities;
            yield return new SourceSchemaBatchResult(i, new SourceSchemaResult(
                CompactPath.Root, SourceResultDocument.Parse(context.Memory, bytes, bytes.Length)));
        }
        await Task.CompletedTask;
    }

    private void Capture(SourceSchemaClientRequest request)
    {
        if (!Capturing)
        {
            return;
        }
        if (request.Variables.Length != 1)
        {
            throw new InvalidOperationException("Expected one combined variable set.");
        }
        using var values = JsonDocument.Parse(request.Variables[0].Values.AsSequence());
        if (values.RootElement.GetProperty("representations").GetArrayLength() != _shape.Unique)
        {
            throw new InvalidOperationException("Wrong unique representation count.");
        }
        CapturedVariables.Add(values.RootElement.GetRawText());
    }

    public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
        OperationPlanContext context, SourceSchemaClientRequest request, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class ClientFactory(ISourceSchemaClient client) : ISourceSchemaClientFactory
{
    public bool CanHandle(ISourceSchemaClientConfiguration configuration) => configuration is ClientConfiguration;
    public ISourceSchemaClient CreateClient(FusionSchemaDefinition schema, ISourceSchemaClientConfiguration configuration) => client;
}

internal sealed class ClientConfiguration(string name) : ISourceSchemaClientConfiguration
{
    public string Name { get; } = name;
    public SupportedOperationType SupportedOperations => SupportedOperationType.Query;
}

internal static class Calibration
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Allocate()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 127; i++)
        {
            GC.KeepAlive(new byte[4096]);
        }
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
