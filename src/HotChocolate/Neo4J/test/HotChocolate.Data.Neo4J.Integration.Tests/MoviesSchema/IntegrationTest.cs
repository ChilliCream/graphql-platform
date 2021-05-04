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
        public void Integration_SchemaSnapshot()
        {
            IRequestExecutor tester = _fixture.CreateSchema();
            tester.Schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task Integration_Query_SchemaSnapshot()
        {
            IRequestExecutor tester = _fixture.CreateSchema();

            IExecutionResult res1 = await tester.ExecuteAsync(
                @"{
                        actors {
                            name
                        }
                    }");

            res1.MatchSnapshot();
        }
    }
}
