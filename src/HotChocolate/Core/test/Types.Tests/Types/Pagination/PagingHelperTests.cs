using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// hc-0-6cq.13 property tests: the cursor and offset batch partition key helpers each read only
/// their own paging model's arguments. A cursor connection field never declares <c>skip</c>/
/// <c>take</c> and an offset segment field never declares <c>first</c>/<c>after</c>/<c>last</c>/
/// <c>before</c>, so <see cref="IResolverContext.ArgumentValue{T}"/> throws for the other model's
/// argument name; these tests call the helper directly from inside a real resolver and prove it
/// never throws.
/// </summary>
public class PagingHelperTests
{
    [Fact]
    public async Task GetPagingBatchPartitionKey_Should_Not_Read_Skip_Argument()
    {
        // arrange
        ulong? partitionKey = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Name("Query")
                .Field("items")
                .UsePaging<ObjectType<Item>>()
                .Resolve(ctx =>
                {
                    partitionKey = PagingHelper.GetPagingBatchPartitionKey((IMiddlewareContext)ctx);
                    return new ValueTask<object?>(new[] { new Item("a"), new Item("b") });
                }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ items(first: 1) { nodes { name } } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
        Assert.NotNull(partitionKey);
    }

    [Fact]
    public async Task GetOffsetPagingBatchPartitionKey_Should_Not_Read_First_Argument()
    {
        // arrange
        ulong? partitionKey = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Name("Query")
                .Field("items")
                .UseOffsetPaging<ObjectType<Item>>()
                .Resolve(ctx =>
                {
                    partitionKey = PagingHelper.GetOffsetPagingBatchPartitionKey((IMiddlewareContext)ctx);
                    return new ValueTask<object?>(new[] { new Item("a"), new Item("b") });
                }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ items(take: 1) { items { name } } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
        Assert.NotNull(partitionKey);
    }

    public record Item(string Name);
}
