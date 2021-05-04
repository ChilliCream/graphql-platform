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
        public async Task Integration_ActorsQuery()
        {
            IRequestExecutor tester = await _fixture.CreateSchema();

            IExecutionResult res1 = await tester.ExecuteAsync(
                @"{
                        actors {
                            name
                            actedIn {
                                title
                            }
                        }
                    }");
            tester.Schema.Print().MatchSnapshot("Integration_ActorsQuery_SchemaSnapshot");
            res1.MatchSnapshot("Integration_ActorsQuery");
        }
    }
}
