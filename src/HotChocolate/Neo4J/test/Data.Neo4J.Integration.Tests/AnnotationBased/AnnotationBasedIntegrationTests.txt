using CookieCrumble;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Integration.AnnotationBased;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class AnnotationBasedIntegrationTests : IClassFixture<SchemaFirst.Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly SchemaFirst.Neo4JFixture _fixture;

    public AnnotationBasedIntegrationTests(Neo4JDatabase database, SchemaFirst.Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    [Fact]
    public async Task MoviesSchemaIntegrationTests_GetSchema()
    {
        // arrange
        var tester = await _fixture.Arrange(_database);

        // assert
        await Snapshot.Create()
            .Add(tester.Schema, "Schema")
            .MatchAsync();
    }

    [Fact(Skip = "Nested sorting doesn't work, causes flaky tests")]
    public async Task MoviesSchemaIntegrationTests()
    {
        // arrange
        var tester = await _fixture.Arrange(_database);

        // act
        var res1 = await tester.ExecuteAsync(
            @"{
                actors {
                    name
                    actedIn {
                        title
                    }
                }
            }");

        // assert
        await Snapshot
            .Create()
            .Add(tester.Schema)
            .Add(res1, "MoviesSchema_Actors_Query")
            .MatchAsync();
    }
}
