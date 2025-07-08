using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Pagination;

public class PagingArgumentsParameterExpressionBuilderTests
{
    [Fact(Skip = "FIXME")]
    public async Task X()
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

        // assert
        Assert.Null(result.ExpectOperationResult().Errors);
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
