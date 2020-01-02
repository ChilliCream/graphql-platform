using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Relay;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace MarshmallowPie.GraphQL.Environments
{
    public class EnvironmentMutationTests
        : GraphQLTestBase
    {
        public EnvironmentMutationTests(MongoResource mongoResource)
            : base(mongoResource)
        {
        }

        [Fact]
        public async Task CreateEnvironment()
        {
            // arrange
            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"mutation {
                            createEnvironment(input: {
                                name: ""abc""
                                description: ""def""
                                clientMutationId: ""ghi"" }) {
                                environment {
                                    id
                                    name
                                    description
                                }
                                clientMutationId
                            }
                        }")
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.NotNull(fo.Field<string>("Data.createEnvironment.environment.id"))));
        }

        [Fact]
        public async Task UpdateEnvironment()
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
                        @"mutation($id: ID!) {
                            updateEnvironment(input: {
                                id: $id
                                name: ""abc2""
                                description: ""def2""
                                clientMutationId: ""ghi"" }) {
                                environment {
                                    id
                                    name
                                    description
                                }
                                clientMutationId
                            }
                        }")
                    .SetVariableValue("id", id)
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.NotNull(fo.Field<string>("Data.updateEnvironment.environment.id"))));
        }
    }
}
