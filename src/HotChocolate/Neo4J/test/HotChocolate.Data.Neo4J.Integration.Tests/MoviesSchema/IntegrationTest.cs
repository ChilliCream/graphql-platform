using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Integration
{
    public class IntegrationTests
        : IClassFixture<Neo4JFixture>
    {
        private readonly Neo4JFixture _fixture;

        public IntegrationTests(Neo4JFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task MoviesSchemaIntegrationTests()
        {
            IRequestExecutor tester = await _fixture.CreateSchema();
            tester.Schema.Print().MatchSnapshot("MoviesSchema_Snapshot");

            IExecutionResult res1 = await tester.ExecuteAsync(
                @"{
                        actors {
                            name
                            actedIn {
                                title
                            }
                        }
                    }");

            res1.MatchSnapshot("MoviesSchema_Actors_Query");

            IExecutionResult res2 = await tester.ExecuteAsync(
                @"{
                        actors (where : {name : { startsWith : ""Keanu"" }}) {
                            name
                            actedIn {
                                title
                            }
                        }
                    }");

            res2.MatchSnapshot("MoviesSchema_Name_StartsWith_Actors_Query");

            IExecutionResult res3 = await tester.ExecuteAsync(
                @"{
                        movies {
                            title
                        }
                    }");
            res3.MatchSnapshot("MoviesSchema_Movies_Query");

            IExecutionResult res4 = await tester.ExecuteAsync(
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
}
