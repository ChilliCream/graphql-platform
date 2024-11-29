using HotChocolate.Execution;
using HotChocolate.Tests;

namespace HotChocolate.Types.Pagination;

public class CustomCursorHandlerTests
{
    [Fact]
    public void Infer_Schema_Correctly_When_Connection_IsUsed()
    {
        SchemaBuilder.New()
            .AddQueryType<Query>()
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public async Task Use_Resolver_Result_If_It_Is_A_Page()
    {
        // arrange
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ items { nodes } }")
                .Build();

        // act
        // assert
        await SchemaBuilder.New()
            .AddQueryType<Query>()
            .Create()
            .MakeExecutable()
            .ExecuteAsync(request)
            .MatchSnapshotAsync();
    }

    public class Query
    {
        [UsePaging]
        public Connection<string> GetItems(
            int first = 10,
            string? after = null,
            int? last = null,
            string? before = null)
        {
            return new(
                new[] { new Edge<string>("hello", "abc"), },
                new ConnectionPageInfo(false, false, "abc", "abc"),
                2000);
        }
    }
}
