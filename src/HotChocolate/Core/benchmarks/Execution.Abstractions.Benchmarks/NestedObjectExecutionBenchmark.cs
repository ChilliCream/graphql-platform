using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Abstractions.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class NestedObjectExecutionBenchmark
{
    private const int ItemCount = 50;

    private const string Document =
        """
        {
            items {
                id
                owner {
                    name
                    address { city country }
                }
                address { city country }
                reviewer { name }
            }
        }
        """;

    private IRequestExecutor _executor = null!;
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
        _executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();
        _request = OperationRequestBuilder.New()
            .SetDocument(Document)
            .Build();

        await using var result = await _executor.ExecuteAsync(_request);

        if (result is not OperationResult { Errors.Count: 0 })
        {
            throw new InvalidOperationException("The nested object request did not execute without errors.");
        }
    }

    [Benchmark]
    public async Task<ExecutionResultKind> Execute_Nested_Objects()
    {
        await using var result = await _executor.ExecuteAsync(_request);
        return result.Kind;
    }

    public sealed class Query
    {
        private readonly Item[] _items = CreateItems();

        public Item[] GetItems() => _items;

        private static Item[] CreateItems()
        {
            var items = new Item[ItemCount];

            for (var i = 0; i < items.Length; i++)
            {
                items[i] = new Item(
                    i,
                    new Person("owner", new Address("Zurich", "CH")),
                    new Address("Bern", "CH"),
                    new Person("reviewer", new Address("Geneva", "CH")));
            }

            return items;
        }
    }

    public sealed record Item(int Id, Person Owner, Address Address, Person Reviewer);

    public sealed record Person(string Name, Address Address);

    public sealed record Address(string City, string Country);
}
