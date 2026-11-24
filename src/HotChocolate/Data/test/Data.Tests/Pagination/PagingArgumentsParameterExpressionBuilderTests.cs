using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Pagination;

public class PagingArgumentsParameterExpressionBuilderTests
{
    [Fact]
    public async Task Maps_NullOrdering_From_PagingOptions_To_PagingArguments()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyPagingOptions(o => o.NullOrdering = NullOrdering.NativeNullsFirst)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ ints { nodes } }", TestContext.Current.CancellationToken);
        var operationResult = result.ExpectOperationResult();

        // assert
        Assert.Empty(operationResult.Errors);
        Assert.Equal(NullOrdering.NativeNullsFirst, Query.PagingArguments.NullOrdering);
    }

    [Theory]
    [InlineData("{ ints { totalCount } }", false)]
    [InlineData("{ ints { totalCount nodes } }", true)]
    [InlineData("{ ints { totalCount edges { cursor } } }", true)]
    [InlineData("{ ints { totalCount pageInfo { hasNextPage } } }", true)]
    [InlineData("{ ints { totalCount pageInfo { __typename } } }", true)]
    public async Task MapArguments_Should_DeriveIncludeItems_When_TheSelectionVaries(string query, bool expected)
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyPagingOptions(o => o.IncludeTotalCount = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
        var operationResult = result.ExpectOperationResult();

        // assert
        Assert.Empty(operationResult.Errors);
        Assert.True(Query.PagingArguments.IncludeTotalCount);
        Assert.Equal(expected, Query.PagingArguments.IncludeItems);
    }

    [Theory]
    [InlineData("{ ints { nodes } }")]
    [InlineData("{ ints { __typename } }")]
    public async Task MapArguments_Should_IncludeItems_When_TotalCountIsNotSelected(string query)
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyPagingOptions(o => o.IncludeTotalCount = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
        var operationResult = result.ExpectOperationResult();

        // assert
        Assert.Empty(operationResult.Errors);
        Assert.False(Query.PagingArguments.IncludeTotalCount);
        Assert.True(Query.PagingArguments.IncludeItems);
    }

    public class Query
    {
        public static PagingArguments PagingArguments { get; private set; }

        [UsePaging]
        public IEnumerable<int> GetInts(PagingArguments pagingArguments)
        {
            PagingArguments = pagingArguments;

            return [];
        }
    }
}
