using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class IntegrationTests
{
    [Fact]
    public async Task Schema_Snapshot()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Subscription_With_Subscribe_With_Delivers_Message_From_Stream()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildRequestExecutorAsync();

        // act
        await using var subscriptionResult = await executor.ExecuteAsync(
            "subscription { onProductAdded(categoryId: 42) }");

        // assert
        var stream = subscriptionResult.ExpectResponseStream();
        await foreach (var result in stream.ReadResultsAsync())
        {
            result.MatchInlineSnapshot(
                """
                {
                  "data": {
                    "onProductAdded": 42
                  }
                }
                """);
            break;
        }
    }

    [Fact]
    public async Task Subscription_With_Public_Subscribe_Source_Is_Not_Exposed_As_Field()
    {
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildSchemaAsync();

        var subscription = schema.Types.GetType<ObjectType>("Subscription");
        Assert.Equal(
            ["onProductAdded", "onProductPriceChanged"],
            subscription.Fields.Where(f => !f.IsIntrospectionField).Select(f => f.Name).ToArray());
    }

    [Fact]
    public async Task Maps_NullOrdering_From_PagingOptions_To_PagingArguments()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .ModifyPagingOptions(o => o.NullOrdering = NullOrdering.NativeNullsFirst)
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ ints { nodes } }");
        var operationResult = result.ExpectOperationResult();

        // assert
        Assert.Empty(operationResult.Errors);
        Assert.Equal(NullOrdering.NativeNullsFirst, Query.PagingArguments.NullOrdering);
    }
}
