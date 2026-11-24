using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Abstractions.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class ExecutionGroupExecutionBenchmark
{
    private const int ConnectionCount = 50;

    private const string Document =
        """
        {
            items {
                connection {
                    nodes { name }
                    pageInfo { hasNextPage }
                }
            }
        }
        """;

    private IRequestExecutor _withoutGroups = null!;
    private IRequestExecutor _withGroups = null!;
    private IOperationRequest _request = null!;

    private sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(
                Job.Default
                    .WithWarmupCount(3)
                    .WithIterationCount(10)
                    .WithInvocationCount(1024)
                    .WithUnrollFactor(1)
                    .WithToolchain(InProcessEmitToolchain.Instance));
        }
    }

    [GlobalSetup]
    public async Task SetupAsync()
    {
        _withoutGroups = await CreateExecutorAsync(withGroups: false);
        _withGroups = await CreateExecutorAsync(withGroups: true);
        _request = OperationRequestBuilder.New()
            .SetDocument(Document)
            .Build();

        var expected = await ExecuteToJsonAsync(_withoutGroups);
        var actual = await ExecuteToJsonAsync(_withGroups);

        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The execution groups changed the result of the request.");
        }
    }

    [Benchmark(Baseline = true)]
    public async Task<ExecutionResultKind> Execute_Without_Groups()
    {
        await using var result = await _withoutGroups.ExecuteAsync(_request);
        return result.Kind;
    }

    [Benchmark]
    public async Task<ExecutionResultKind> Execute_With_Groups()
    {
        await using var result = await _withGroups.ExecuteAsync(_request);
        return result.Kind;
    }

    private async Task<string> ExecuteToJsonAsync(IRequestExecutor executor)
    {
        await using var result = await executor.ExecuteAsync(_request);

        if (result is not OperationResult { Errors.Count: 0 })
        {
            throw new InvalidOperationException("The execution group request did not execute without errors.");
        }

        return result.ToJson(withIndentations: false);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(bool withGroups)
        => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddObjectType<Item>(descriptor =>
            {
                var connection = descriptor.Field(item => item.GetConnection());

                if (withGroups)
                {
                    connection.ExecutionGroup(
                        new ExecutionGroupEntry("nodes", 0, isStreamable: true),
                        new ExecutionGroupEntry("pageInfo.hasNextPage", 1, isStreamable: false));
                }
            })
            .BuildRequestExecutorAsync();

    public sealed class Query
    {
        private readonly Item[] _items = CreateItems();

        public Item[] GetItems() => _items;

        private static Item[] CreateItems()
        {
            var items = new Item[ConnectionCount];

            for (var i = 0; i < items.Length; i++)
            {
                items[i] = new Item();
            }

            return items;
        }
    }

    public sealed class Item
    {
        public Connection GetConnection() => new();
    }

    public sealed class Connection
    {
        private static readonly Node[] s_nodes = [new("a"), new("b"), new("c")];

        public ValueTask<Node[]> GetNodesAsync() => new(s_nodes);

        public PageInfo GetPageInfo() => new();
    }

    public sealed class PageInfo
    {
        public ValueTask<bool> GetHasNextPageAsync() => new(true);
    }

    public sealed record Node(string Name);
}
