using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

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
        var tester = await _fixture.CreateSchema();
        tester.Schema.Print().MatchSnapshot("MoviesSchema_Snapshot");

        var res1 = await tester.ExecuteAsync(
            @"{
                        actors {
                            name
                            actedIn {
                                title
                            }
                        }
                    }");

        res1.MatchSnapshot("MoviesSchema_Actors_Query");

        var res2 = await tester.ExecuteAsync(
            @"{
                        actors (where : {name : { startsWith : ""Keanu"" }}) {
                            name
                            actedIn {
                                title
                            }
                        }
                    }");

        res2.MatchSnapshot("MoviesSchema_Name_StartsWith_Actors_Query");

        var res3 = await tester.ExecuteAsync(
            @"{
                        movies {
                            title
                        }
                    }");
        res3.MatchSnapshot("MoviesSchema_Movies_Query");

        var res4 = await tester.ExecuteAsync(
            @"{
                        actors(order: [{ name : ASC }]) {
                            name
                            actedIn {
                                title
                            }
                        }
                    }");
        res4.MatchSnapshot("MoviesSchema_Name_Desc_Sort_Actors_Query");
    }
}