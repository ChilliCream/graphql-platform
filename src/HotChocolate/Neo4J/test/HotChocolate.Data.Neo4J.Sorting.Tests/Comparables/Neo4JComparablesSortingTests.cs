using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Neo4J.Sorting.Boolean;

[Collection("Database")]
public class Neo4JComparablesSortingTests
{
    private readonly Neo4JFixture _fixture;

    public Neo4JComparablesSortingTests(Neo4JFixture fixture)
    {
        _fixture = fixture;
    }

    private string _fooEntitiesCypher = @"
            CREATE (:FooComp {Bar: 12}), (:FooComp {Bar: 14}), (:FooComp {Bar: 13})
        ";

    public class FooComp
    {
        public short Bar { get; set; }
    }

    public class FooCompSortType
        : SortInputType<FooComp>
    {
    }

    [Fact]
    public async Task Create_Short_OrderBy()
    {
        // arrange
        IRequestExecutor tester =
            await _fixture.GetOrCreateSchema<FooComp, FooCompSortType>(_fooEntitiesCypher);

        // act
        IExecutionResult res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(order: { bar: ASC}){ bar}}")
                .Create());

        IExecutionResult res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(order: { bar: DESC}){ bar}}")
                .Create());

        // assert
        res1.MatchDocumentSnapshot("ASC");
        res2.MatchDocumentSnapshot("DESC");
    }
}
