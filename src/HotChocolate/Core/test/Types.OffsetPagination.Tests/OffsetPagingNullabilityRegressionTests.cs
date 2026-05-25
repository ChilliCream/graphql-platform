using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

public class OffsetPagingNullabilityRegressionTests
{
    [Fact]
    public async Task OffsetPaging_TaskEnumerableOfNonNullReferenceType_Infers_NonNull_Items()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        // act
        var segmentType = schema.Types.GetType<ObjectType>("FoosCollectionSegment");
        var itemsType = segmentType.Fields["items"].Type;

        // assert
        Assert.False(itemsType.IsNonNullType());
        var listType = Assert.IsType<ListType>(itemsType);
        Assert.True(listType.ElementType.IsNonNullType());
        Assert.Equal("Foo", listType.ElementType.NamedType().Name);
    }

    public class Query
    {
        [UseOffsetPaging]
        public async Task<IEnumerable<Foo>> GetFoos()
        {
            await Task.Yield();
            return [];
        }
    }

    public record Foo(string Bar);
}
