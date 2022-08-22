using CookieCrumble;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Integration.Tests.SchemaFirst;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class SchemaIntegrationTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public SchemaIntegrationTests(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    [Fact]
    public async Task MoviesSchemaIntegrationTests_GetSchema()
    {
        // arrange
        var tester = await _fixture.Arrange(_database);

        await Snapshot.Create()
            .Add(tester.Schema, "Schema")
            .MatchAsync();
    }

    [Fact]
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

        var res2 = await tester.ExecuteAsync(
            @"{
                actors (where : {name : { startsWith : ""Keanu"" }}) {
                    name
                    actedIn {
                        title
                    }
                }
            }");

        var res3 = await tester.ExecuteAsync(
            @"{
                movies {
                    title
                }
            }");

        var res4 = await tester.ExecuteAsync(
            @"{
                actors(order: [{ name : ASC }]) {
                    name
                    actedIn {
                        title
                    }
                }
            }");

        var res5 = await tester.ExecuteAsync(@"{
                actors(order: [{ name : ASC }] skip: 1 take: 2) {
                    items {
                        name
                        actedIn {
                            title
                        }
                    }
                }
            }");

        // assert
        await Snapshot.Create()
            .AddResult(res1, "MoviesSchema_Actors_Query")
            .AddResult(res2, "MoviesSchema_Name_StartsWith_Actors_Query")
            .AddResult(res3, "MoviesSchema_Movies_Query")
            .AddResult(res4, "MoviesSchema_Name_Desc_Sort_Actors_Query")
            .AddResult(res5, "MoviesSchema_Name_Desc_Sort_Actors_SkipOne_TakeTwo_Query")
            .MatchAsync();
    }
}
