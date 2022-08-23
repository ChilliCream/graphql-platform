using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Neo4J.Sorting.Boolean;

[Collection("Database")]
public class Neo4JBooleanSortingTests
{
    private readonly Neo4JFixture _fixture;

    public Neo4JBooleanSortingTests(Neo4JFixture fixture)
    {
        _fixture = fixture;
    }

    private string _fooEntitiesCypher = @"
            CREATE (:FooBool {Bar: true}), (:FooBool {Bar: false})
        ";

    public class FooBool
    {
        public bool Bar { get; set; }
    }

    public class FooBoolSortType : SortInputType<FooBool>
    {
    }

    [Fact]
    public async Task Create_Boolean_OrderBy()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooBool, FooBoolSortType>(_fooEntitiesCypher);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(order: { bar: ASC}){ bar}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(order: { bar: DESC}){ bar}}")
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }
}
