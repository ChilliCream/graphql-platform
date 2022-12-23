using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Filtering;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class Neo4JFilterCombinatorTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public Neo4JFilterCombinatorTests(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    private const string FooEntitiesCypher =
        @"CREATE (:FooBool {Bar: true}), (:FooBool {Bar: false})";

    [Fact]
    public async Task FilterCombinator_Empty()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooBool, FooBoolFilterType>( _database, FooEntitiesCypher);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { }){ bar }}")
                .Create());

        await Snapshot.Create()
            .Add(res1)
            .MatchAsync();
    }

    public class FooBool
    {
        public bool Bar { get; set; }
    }

    public class FooBoolFilterType : FilterInputType<FooBool>
    {
    }
}
