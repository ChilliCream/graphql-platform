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
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ ints { nodes } }");
        var operationResult = result.ExpectOperationResult();

        // assert
        Assert.Empty(operationResult.Errors);
        Assert.Equal(NullOrdering.NativeNullsFirst, Query.PagingArguments.NullOrdering);
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
