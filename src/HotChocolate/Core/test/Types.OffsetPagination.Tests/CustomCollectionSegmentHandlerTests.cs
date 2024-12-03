using HotChocolate.Execution;
using HotChocolate.Tests;

namespace HotChocolate.Types.Pagination;

public class CustomCollectionSegmentHandlerTests
{
    [Fact]
    public void Infer_Schema_Correctly_When_CollectionSegment_IsUsed()
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
                .SetDocument("{ items { items } }")
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
        [UseOffsetPaging]
        public CollectionSegment<string> GetItems(int skip, int take)
        {
            return new CollectionSegment<string>(
                new[] { "hello", "abc", },
                new CollectionSegmentInfo(false, false),
                1);
        }
    }
}
