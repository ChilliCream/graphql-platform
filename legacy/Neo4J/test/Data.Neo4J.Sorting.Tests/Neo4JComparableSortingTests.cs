using CookieCrumble;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;

namespace HotChocolate.Data;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class Neo4JComparableSortingTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public Neo4JComparableSortingTests(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    private const string _fooEntitiesCypher =
        @"CREATE (:FooComp {Bar: 12}), (:FooComp {Bar: 14}), (:FooComp {Bar: 13})";

    [Fact]
    public async Task ComparableSorting_SchemaSnapshot()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompSortType>(_database, _fooEntitiesCypher);

        tester.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task ComparableSorting_Short()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompSortType>(_database, _fooEntitiesCypher);

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
        await Snapshot
            .Create()
            .AddResult(res1, "ComparableSorting_Short_ASC")
            .AddResult(res2, "ComparableSorting_Short_DESC")
            .MatchAsync();
    }

    public class FooComp
    {
        public short Bar { get; set; }
    }

    public class FooCompSortType : SortInputType<FooComp>
    {
    }
}
