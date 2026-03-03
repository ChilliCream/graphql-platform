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
