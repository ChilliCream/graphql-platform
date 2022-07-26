using CookieCrumble;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Integration.AnnotationBased;

public class AnnotationBasedIntegrationTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JFixture _fixture;

    public AnnotationBasedIntegrationTests(Neo4JFixture fixture)
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
        await Snapshot
            .Create()
            .Add(tester.Schema)
            .Add(res1, "MoviesSchema_Actors_Query")
            .Add(res2, "MoviesSchema_Name_StartsWith_Actors_Query")
            .Add(res3, "MoviesSchema_Movies_Query")
            .Add(res4, "MoviesSchema_Name_Desc_Sort_Actors_Query")
            .MatchAsync();
    }
}
