using CookieCrumble;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Sorting.Tests.Boolean;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class Neo4JBooleanSortingTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public Neo4JBooleanSortingTests(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    private string _fooEntitiesCypher = @"
            CREATE (:FooBool {Bar: true}), (:FooBool {Bar: false})
        ";

    [Fact]
    public async Task Create_Boolean_OrderBy()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooBool, FooBoolSortType>(_database, _fooEntitiesCypher);

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

    public class FooBool
    {
        public bool Bar { get; set; }
    }

    public class FooBoolSortType : SortInputType<FooBool>
    {
    }
}
