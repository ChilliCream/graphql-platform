using System.Collections.Concurrent;
using System.Text.Json;
using CookieCrumble;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class VariableBatchBatchResolverTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DeferredBatch_Should_RegisterAllProducers_When_VariableSetsShareSelection(
        bool exceedsBuffer,
        bool reportError)
    {
        // arrange
        var count = exceedsBuffer ? Environment.ProcessorCount * 2 + 1 : 2;
        var sizes = new ConcurrentQueue<int>();
        var arguments = new ConcurrentQueue<int>();
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Field("productById")
                .Argument("id", a => a.Type<NonNullType<IntType>>())
                .Type<ObjectType<BatchProduct>>()
                .ResolveBatch(contexts =>
                {
                    sizes.Enqueue(contexts.Count);
                    return new ValueTask<IReadOnlyList<ResolverResult>>(contexts.Select(context =>
                    {
                        var id = context.ArgumentValue<int>("id");
                        arguments.Enqueue(id);
                        if (reportError && id == 2)
                        {
                            context.ReportError("Product 2 failed.");
                        }

                        return ResolverResult.Ok(new BatchProduct(id, $"Product {id}"));
                    }).ToArray());
                }))
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync(OperationRequestBuilder.New()
            .SetDocument("query($id:Int!){ ... @defer { productById(id:$id){name} } }")
            .SetVariableValues(Enumerable.Range(1, count)
                .Select(id => (IReadOnlyDictionary<string, object?>)
                    new Dictionary<string, object?> { ["id"] = id }).ToList())
            .Build(), cancellationToken: TestContext.Current.CancellationToken);
        var batch = Assert.IsType<OperationResultBatch>(result);
        var outcomes = await Task.WhenAll(batch.Results.Select(ReadDeferredSetAsync));

        // assert
        Assert.Equal(new[] { count }, sizes.ToArray());
        Assert.Equal(Enumerable.Range(1, count), arguments.Order());
        Assert.Equal(Enumerable.Range(1, count).Select(id => new DeferredSet(
            id - 1,
            $"Product {id}",
            reportError && id == 2 ? "Product 2 failed." : "")), outcomes);
        if (!exceedsBuffer)
        {
            new Snapshot(postFix: reportError.ToString())
                .Add(outcomes, "Per-set payloads and errors")
                .Add(arguments.Order().ToArray(), "Per-set arguments")
                .Add(sizes.ToArray(), "Invocations")
                .MatchMarkdownSnapshot();
        }
    }

    private static async Task<DeferredSet> ReadDeferredSetAsync(IExecutionResult result)
    {
        var index = -1;
        var name = "";
        var errors = new List<string>();
        await foreach (var response in ((ResponseStream)result).ReadResultsAsync())
        {
            await using (response)
            {
                using var document = JsonDocument.Parse(response.ToJson());
                Visit(document.RootElement);
            }
        }

        return new DeferredSet(index, name, string.Join(";", errors));

        void Visit(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    switch (property.Name)
                    {
                        case "variableIndex":
                            index = property.Value.GetInt32();
                            break;
                        case "name":
                            name = property.Value.GetString()!;
                            break;
                        case "message":
                            errors.Add(property.Value.GetString()!);
                            break;
                        default:
                            Visit(property.Value);
                            break;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    Visit(item);
                }
            }
        }
    }

    private sealed record DeferredSet(int VariableIndex, string Name, string Errors);

    [Theory]
    [InlineData("none", 0)]
    [InlineData("null", 1)]
    [InlineData("null", 2)]
    [InlineData("error", 2)]
    [InlineData("throw", 0)]
    [InlineData("partition", 0)]
    [InlineData("partition", 1)]
    [InlineData("partition", 2)]
    public async Task VariableBatch_Should_Complete_In_Owning_Context_When_Batch_Entries_Succeed_Or_Fail(
        string failure,
        int failingId)
    {
        // arrange
        var batchSizes = new List<int>();
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Name("Query");
                var field = d.Field("productById")
                    .Argument("id", a => a.Type<NonNullType<IntType>>())
                    .Type<NonNullType<ObjectType<BatchProduct>>>()
                    .ResolveBatch(contexts =>
                    {
                        batchSizes.Add(contexts.Count);

                        if (failure == "throw")
                        {
                            throw new InvalidOperationException("Batch failed.");
                        }

                        var results = new ResolverResult[contexts.Count];

                        for (var i = 0; i < contexts.Count; i++)
                        {
                            var id = contexts[i].ArgumentValue<int>("id");

                            if (id == failingId)
                            {
                                if (failure == "error")
                                {
                                    contexts[i].ReportError($"Product {id} failed.");
                                }

                                results[i] = ResolverResult.Ok(null);
                            }
                            else
                            {
                                results[i] = ResolverResult.Ok(new BatchProduct(id, $"Product {id}"));
                            }
                        }

                        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                    });

                if (failure == "partition")
                {
                    field.Extend().Configuration.BatchPartitionKeyResolver = context =>
                    {
                        var id = context.ArgumentValue<int>("id");

                        if (id == failingId)
                        {
                            throw new InvalidOperationException($"Partition {id} failed.");
                        }

                        return (ulong)id;
                    };
                }
            })
            .AddObjectType<BatchProduct>(d =>
            {
                d.Field(p => p.Id);
                d.Field(p => p.Name);
                d.Field("argument")
                    .Argument("id", a => a.Type<NonNullType<IntType>>())
                    .Type<NonNullType<IntType>>()
                    .Resolve(async context =>
                    {
                        await Task.Yield();
                        return context.ArgumentValue<int>("id");
                    });
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($id: Int!) {
                        productById(id: $id) {
                            name
                            argument(id: $id)
                        }
                    }
                    """)
                .SetVariableValues(
                    new List<IReadOnlyDictionary<string, object?>>
                    {
                        new Dictionary<string, object?> { { "id", 1 } },
                        new Dictionary<string, object?> { { "id", 2 } }
                    })
                .Build(),
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var batch = Assert.IsType<OperationResultBatch>(result);
        new Snapshot(postFix: $"{failure}_{failingId}")
            .Add(batch.Results[0], "Set 0")
            .Add(batch.Results[1], "Set 1")
            .Add(batchSizes, "Batch sizes")
            .MatchMarkdownSnapshot();
    }

    public record BatchProduct(int Id, string Name);
}
