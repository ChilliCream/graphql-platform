using CookieCrumble;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Integration.SchemaFirst;

public class SchemaIntegrationTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JFixture _fixture;

    public SchemaIntegrationTests(Neo4JFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MoviesSchemaIntegrationTests()
    {
        // arrange
        var tester = await _fixture.CreateSchema();

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

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create()
                                .Add(tester.Schema, "MoviesSchema_Snapshot"), res1, "MoviesSchema_Actors_Query"), res2, "MoviesSchema_Name_StartsWith_Actors_Query"), res3, "MoviesSchema_Movies_Query"), res4, "MoviesSchema_Name_Desc_Sort_Actors_Query")
            .MatchAsync();
    }
}
