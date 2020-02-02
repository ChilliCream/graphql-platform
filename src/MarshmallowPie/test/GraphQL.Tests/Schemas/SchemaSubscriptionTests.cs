using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Relay;
using MarshmallowPie.Processing;
using MarshmallowPie.Storage.FileSystem;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace MarshmallowPie.GraphQL.Schemas
{
    public class SchemaSubscriptionTests
        : GraphQLIntegrationTestBase
    {
        public SchemaSubscriptionTests(
            MongoResource mongoResource,
            FileStorageResource fileStorageResource)
            : base(mongoResource, fileStorageResource)
        {
        }

        [Fact]
        public async Task OnPublishSchema()
        {
            // arrange
            var serializer = new IdSerializer();

            var schema = new Schema("abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);

            var environment = new Environment("abc", "def");
            await EnvironmentRepository.AddEnvironmentAsync(environment);

            string sessionId = "abc";

            await PublishSchemaEventSender.SendAsync(
                new PublishSchemaEvent(sessionId, new Issue("foo", IssueType.Information)));

            await PublishSchemaEventSender.SendAsync(
                PublishSchemaEvent.Completed(sessionId));

            // act
            var responseStream = (IResponseStream)await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"subscription(
                            $sessionId: String!) {
                            onPublishSchema(sessionId: $sessionId) {
                                sessionId
                                isCompleted
                            }
                        }")
                    .SetVariableValue("sessionId", sessionId)
                    .Create());

            // assert
            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in responseStream)
            {
                results.Add(result);
            }
            results.MatchSnapshot();
        }
    }
}
