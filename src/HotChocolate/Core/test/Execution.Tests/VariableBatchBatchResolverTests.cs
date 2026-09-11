using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using CookieCrumble;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class VariableBatchBatchResolverTests
{
    [Theory]
    [InlineData(false, 0, false)]
    [InlineData(true, 0, false)]
    [InlineData(false, 64, false)]
    [InlineData(true, 64, false)]
    [InlineData(false, 128, false)]
    [InlineData(true, 128, false)]
    [InlineData(false, 128, true)]
    [InlineData(true, 128, true)]
    public async Task BatchSelection_Should_UnionIncludedMembers_When_ConditionsDiffer(
        bool attributes,
        int padding,
        bool partition)
    {
        // arrange
        var log = new SelectionLog();
        var builder = new ServiceCollection().AddSingleton(log).AddGraphQL();

        if (attributes)
        {
            builder.AddQueryType<SelectionQuery>(d => ObserveFlags(d.Field(
                t => t.GetProduct(default!, default!, default!, false, false, false, false, default!))));
        }
        else
        {
            builder.AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("noop").Resolve("unused");
                d.Field("product").ResolveBatchWith(
                    typeof(SelectionQuery).GetMethod(nameof(SelectionQuery.GetProduct))!);
                ObserveFlags(d.Field("product"));
            });
        }

        var executor = await builder.BuildRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var document = new StringBuilder("query($id:Int!,$skip:Boolean!,$take:Boolean!,$excluded:Boolean!");

        for (var i = 0; i < padding; i++)
        {
            document.Append($",$p{i}:Boolean!");
        }

        document.Append(") {");

        for (var i = 0; i < padding; i++)
        {
            document.Append($" p{i}:noop @include(if:$p{i})");
        }

        document.Append(" product(id:$id) @skip(if:$skip) { id left @include(if:$take) right @skip(if:$take) excluded @include(if:$excluded) } }");
        var sets = new List<IReadOnlyDictionary<string, object?>>();

        for (var id = 1; id <= 3; id++)
        {
            var variables = new Dictionary<string, object?>
            {
                ["id"] = id,
                ["skip"] = id == 3,
                ["take"] = id == 2,
                ["excluded"] = id == 3
            };

            for (var i = 0; i < padding; i++)
            {
                variables[$"p{i}"] = false;
            }

            sets.Add(variables);
        }

        // act
        await using var result = await executor.ExecuteAsync(OperationRequestBuilder.New()
            .SetDocument(document.ToString())
            .SetVariableValues(sets)
            .Build(), cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var batch = Assert.IsType<OperationResultBatch>(result);
        new Snapshot(postFix: $"{attributes}_{padding}{(partition ? "_Partitioned" : "")}")
            .Add(batch.Results[0], "Set 0")
            .Add(batch.Results[1], "Set 1")
            .Add(batch.Results[2], "Skipped set")
            .Add(log.Invocations, "Union binding")
            .MatchMarkdownSnapshot();
        Assert.Equal(partition ? new[] { 1, 1 } : [2], log.Invocations.Select(t => t.Ids.Count));
        Assert.Equal(partition ? new[] { true, true } : [true], log.FlagsUnchanged);
        Assert.Equal(partition ? new[] { true, true } : [true], log.ExternalBindingMatches);

        void ObserveFlags(IObjectFieldDescriptor descriptor)
        {
            if (partition)
            {
                descriptor.Extend().Configuration.BatchPartitionKeyResolver =
                    context => (ulong)context.ArgumentValue<int>("id");
            }

            descriptor.UseBatch(next => async contexts =>
            {
                var before = contexts.Select(c => new[] { c.IncludeConditionFlags.Word0 }
                    .Concat(c.IncludeConditionFlags.Overflow ?? []).ToArray()).ToArray();
                var binding = ResolverContextExtensions.CreateBatchSelectionContext(contexts);
                log.ExternalBindingMatches.Add(new[] { "left", "right", "excluded" }.All(name =>
                    binding.Select().IsSelected(name) == contexts.Any(c => c.Select().IsSelected(name))));
                await next(contexts);
                log.FlagsUnchanged.Add(contexts.Select((c, i) => new[] { c.IncludeConditionFlags.Word0 }
                    .Concat(c.IncludeConditionFlags.Overflow ?? []).SequenceEqual(before[i])).All(t => t));
            });
        }
    }

    public sealed class SelectionLog
    {
        public List<SelectionObservation> Invocations { get; } = [];
        public List<bool> FlagsUnchanged { get; } = [];
        public List<bool> ExternalBindingMatches { get; } = [];
    }

    public sealed record SelectionObservation(
        IReadOnlyList<int> Ids,
        bool Left,
        bool Right,
        bool Excluded,
        bool SelectLeft,
        bool SelectRight,
        bool SelectExcluded,
        bool Pattern,
        bool SameSelection);

    public class SelectionQuery
    {
        public string GetNoop() => "unused";

        [BatchResolver]
        public List<SelectionProduct> GetProduct(
            List<int> id,
            IResolverContext context,
            ISelection selection,
            [IsSelected("left")] bool left,
            [IsSelected("right")] bool right,
            [IsSelected("excluded")] bool excluded,
            [IsSelected("left right")] bool both,
            [Service] SelectionLog log)
        {
            log.Invocations.Add(new SelectionObservation(
                id,
                left,
                right,
                excluded,
                context.Select().IsSelected("left"),
                context.Select("right").Count == 1,
                context.Select().IsSelected("excluded"),
                both,
                ReferenceEquals(selection, context.Selection)));
            return id.Select(i => new SelectionProduct(i, left ? "left" : "missing", right ? "right" : "missing", "excluded")).ToList();
        }
    }

    public record SelectionProduct(int Id, string Left, string Right, string Excluded);

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
