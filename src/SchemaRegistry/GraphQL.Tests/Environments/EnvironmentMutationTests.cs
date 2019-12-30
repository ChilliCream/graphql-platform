using System.Threading.Tasks;
using HotChocolate.Execution;
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
    }
}
