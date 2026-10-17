using System.Security.Cryptography;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Errors;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Performance;

[MemoryDiagnoser]
[Config(typeof(ListCompletionConfig))]
public class ListCompletionBenchmark
{
    private FusionSchemaDefinition _schema = null!;
    private Operation _operation = null!;
    private ResultSelectionSet _selectionSet = null!;
    private MemoryArena _sourceArena = null!;
    private SourceResultDocument _source = null!;
    private FetchResultStore _store = null!;
    private ValueCompletion _completion = null!;

    [Params(1000, 20000)]
    public int Rows { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var assembly = typeof(ValueCompletion).Assembly.Location;
        var hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly)));
        Console.WriteLine($"Measured Fusion assembly: {assembly}; SHA256={hash}");
        if (hash != Environment.GetEnvironmentVariable("FUSION_PERF_ASSEMBLY_SHA256"))
        {
            throw ThrowHelper.InvalidFixture();
        }

        _schema = FusionSchemaDefinition.Create(Utf8GraphQLParser.Parse(
            File.ReadAllText(Environment.GetEnvironmentVariable("FUSION_PERF_SCHEMA_PATH")!)));
        var definition = Utf8GraphQLParser.Parse(
            $"{{shapeProducts(count:{Rows}){{upc name price weight}}}}")
            .Definitions.OfType<OperationDefinitionNode>().Single();
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        _operation = new OperationCompiler(_schema, pool).Compile("micro", "micro", "micro", definition);
        _selectionSet = ResultSelectionSet.Create(definition.SelectionSet, _schema);
        var data = JsonSerializer.SerializeToUtf8Bytes(new
        {
            shapeProducts = Enumerable.Range(1, Rows)
                .Select(i => new { upc = i.ToString(), name = "Product", price = i, weight = i })
        });
        _sourceArena = new MemoryArena();
        _source = SourceResultDocument.Parse(_sourceArena, data, data.Length);
        _sourceArena.Seal();
        _store = new FetchResultStore();
        _completion = new ValueCompletion(_store, _schema, DefaultErrorHandler.Default,
            ErrorHandlingMode.Propagate, 32);
        Complete(validate: true);
    }

    [Benchmark]
    public int Complete() => Complete(validate: false);

    private int Complete(bool validate)
    {
        using var arena = new MemoryArena();
        _store.Initialize(arena, _schema, DefaultErrorHandler.Default, _operation,
            ErrorHandlingMode.Propagate, default, default, 16);
        _source.Id = -1;
        if (!_completion.BuildResult(_source.Root, _store.Result.Data, null, _selectionSet))
        {
            throw ThrowHelper.InvalidFixture();
        }

        var products = _store.Result.Data.GetProperty("shapeProducts");
        var count = products.GetArrayLength();
        if (count != Rows || _store.Errors is { Count: > 0 })
        {
            throw ThrowHelper.InvalidFixture();
        }

        if (validate)
        {
            var index = 1;
            foreach (var product in products.EnumerateArray())
            {
                if (product.GetProperty("upc").GetString() != index.ToString()
                    || product.GetProperty("name").GetString() != "Product"
                    || product.GetProperty("price").GetInt32() != index
                    || product.GetProperty("weight").GetInt32() != index)
                {
                    throw ThrowHelper.InvalidFixture();
                }

                index++;
            }
        }

        _store.Clean(256, 256);
        arena.Seal();
        return count;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _store.Dispose();
        _source.Dispose();
        _sourceArena.Dispose();
    }
}

public sealed class ListCompletionConfig : ManualConfig
{
    public ListCompletionConfig()
    {
        AddJob(Job.Default.WithRuntime(CoreRuntime.Core10_0).WithGcServer(true).WithGcConcurrent(true)
            .WithEnvironmentVariable("DOTNET_GCDynamicAdaptationMode", "0")
            .WithLaunchCount(3).WithWarmupCount(5).WithIterationCount(15));
    }
}

internal static class ThrowHelper
{
    internal static InvalidOperationException InvalidFixture()
        => new("The completion benchmark must use the expected assembly and preserve every product value.");
}
