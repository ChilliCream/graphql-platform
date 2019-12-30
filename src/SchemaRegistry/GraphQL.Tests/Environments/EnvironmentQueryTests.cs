using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Relay;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace MarshmallowPie.GraphQL.Environments
{
    public class EnvironmentQueryTests
            : GraphQLTestBase
    {
        public EnvironmentQueryTests(MongoResource mongoResource)
            : base(mongoResource)
        {
        }

        [Fact]
        public async Task GetNode()
        {
            // arrange
            var serializer = new IdSerializer();
            var environment = new Environment(Guid.NewGuid(), "abc", "def");
            await EnvironmentRepository.AddEnvironmentAsync(environment);
            string id = serializer.Serialize("Environment", environment.Id);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query($id: ID!) {
                            node(id: $id) {
                                id
                                ... on Environment {
                                    name
                                    description
                                }
                            }
                        }")
                    .SetVariableValue("id", id)
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.Equal(id, fo.Field<string>("Data.node.id"))));
        }

        [Fact]
        public async Task GetEnvironmentById()
        {
            // arrange
            var serializer = new IdSerializer();
            var environment = new Environment(Guid.NewGuid(), "abc", "def");
            await EnvironmentRepository.AddEnvironmentAsync(environment);
            string id = serializer.Serialize("Environment", environment.Id);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query($id: ID!) {
                            environmentById(id: $id) {
                                id
                                name
                                description
                            }
                        }")
                    .SetVariableValue("id", id)
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.Equal(id, fo.Field<string>("Data.environmentById.id"))));
        }

        [Fact]
        public async Task GetEnvironmentsById()
        {
            // arrange
            var serializer = new IdSerializer();
            var environment = new Environment(Guid.NewGuid(), "abc", "def");
            await EnvironmentRepository.AddEnvironmentAsync(environment);
            string id = serializer.Serialize("Environment", environment.Id);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query($ids: [ID!]!) {
                            environmentsById(ids: $ids) {
                                id
                                name
                                description
                            }
                        }")
                    .SetVariableValue("ids", new[] { id })
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.Equal(id, fo.Field<string>("Data.environmentsById[0].id"))));
        }

        [Fact]
        public async Task GetEnvironmentsByName()
        {
            // arrange
            var serializer = new IdSerializer();
            var environment = new Environment(Guid.NewGuid(), "abc", "def");
            await EnvironmentRepository.AddEnvironmentAsync(environment);
            string id = serializer.Serialize("Environment", environment.Id);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query($name: String!) {
                            environments(where: { name: $name }) {
                                nodes {
                                    id
                                    name
                                    description
                                }
                            }
                        }")
                    .SetVariableValue("name", environment.Name)
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.Equal(id, fo.Field<string>("Data.environments.nodes[0].id"))));
        }
    }
}
