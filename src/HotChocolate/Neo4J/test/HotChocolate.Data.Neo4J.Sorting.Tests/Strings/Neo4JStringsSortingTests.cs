using CookieCrumble;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Sorting.Tests.Strings;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class Neo4JStringsSortingTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public Neo4JStringsSortingTests(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    private string _fooEntitiesCypher = @"
            CREATE (:FooString {Bar: 'testatest'}), (:FooString {Bar: 'testbtest'})
        ";

    [Fact]
    public async Task Sorting_Strings_SchemaSnapshot()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooString, FooStringSortType>(_database, _fooEntitiesCypher);

        tester.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task Create_String_OrderBy()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooString, FooStringSortType>(_database, _fooEntitiesCypher);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(order: { bar: ASC}){ bar }}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(order: { bar: DESC}){ bar }}")
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }

    public class FooString
    {
        public string Bar { get; set; }
    }

    public class FooStringSortType : SortInputType<FooString>
    {
    }
}
