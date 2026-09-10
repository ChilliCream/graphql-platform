using System.Text.Json;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

// REPRO tests for the known break "variable batching coalesces batch fields across variable
// sets and coerces every set's arguments with set 0's variables". All per-set OperationContexts
// share the first set's WorkScheduler, so identical Selection/FieldSelectionPath instances
// coalesce into ONE BatchResolverTask whose contexts coerce arguments through set 0's variables.
// Sets 1..N-1 silently receive set 0's argument values. These tests assert each set gets its
// own correct data and therefore fail today.
public class VariableBatchBatchResolverTests
{
    [Fact]
    public async Task VariableBatch_Should_Resolve_PerSet_Arguments_When_FieldIsBatchResolved()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("productById")
                        .Argument("id", a => a.Type<NonNullType<IntType>>())
                        .Type<ObjectType<BatchProduct>>()
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var id = contexts[i].ArgumentValue<int>("id");
                                results[i] = ResolverResult.Ok(new BatchProduct(id, $"Product {id}"));
                            }

                            return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                        });
                })
                .AddObjectType<BatchProduct>(d =>
                {
                    d.Field(p => p.Id);
                    d.Field(p => p.Name);
                })
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($id: Int!) {
                        productById(id: $id) {
                            name
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
        // Each variable set must execute with its own $id, so set 0 yields Product 1 and set 1
        // yields Product 2. Today both results return Product 1 (set 0's coerced argument).
        var batch = Assert.IsType<OperationResultBatch>(result);
        var names = batch.Results.Select(GetProductName).ToArray();
        Assert.Equal(new[] { "Product 1", "Product 2" }, names);
    }

    private static string? GetProductName(IExecutionResult result)
    {
        var json = Assert.IsType<OperationResult>(result).ToJson();
        using var document = JsonDocument.Parse(json);
        return document.RootElement
            .GetProperty("data")
            .GetProperty("productById")
            .GetProperty("name")
            .GetString();
    }

    public record BatchProduct(int Id, string Name);
}
