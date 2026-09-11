using CookieCrumble;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class VariableBatchBatchResolverTests
{
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
