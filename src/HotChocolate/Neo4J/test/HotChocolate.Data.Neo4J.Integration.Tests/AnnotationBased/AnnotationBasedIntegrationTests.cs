using CookieCrumble;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Integration.Tests.AnnotationBased;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class AnnotationBasedIntegrationTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public AnnotationBasedIntegrationTests(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    [Fact]
    public async Task MoviesSchemaIntegrationTests_GetSchema()
    {
        var tester = await _fixture.CreateSchema(_database);
        tester.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task MoviesSchemaIntegrationTests()
    {
        // arrange
        var tester = await _fixture.CreateSchema(_database);

        // act
        var res1 = await tester.ExecuteAsync(@"{
                actors {
                    items {
                        name
                        actedIn {
                            title
                        }
                    }
                }
            }");

        var res2 = await tester.ExecuteAsync(@"{
                actors (where : {name : { startsWith : ""Keanu"" }}) {
                    items {
                        name
                        actedIn {
                            title
                        }
                    }
                }
            }");

        var res3 = await tester.ExecuteAsync(@"{
                movies {
                    items {
                        title
                    }
                }
            }");

        var res4 = await tester.ExecuteAsync(@"{
                actors(order: [{ name : ASC }]) {
                    items {
                        name
                        actedIn {
                            title
                        }
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
