using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
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

            string sessionId = await SessionCreator.CreateSessionAsync();

            await PublishSchemaEventSender.SendAsync(
                new PublishDocumentEvent(
                    sessionId,
                    new Issue("foo", "file.graphql", new Location(0, 0, 0, 0),
                    IssueType.Information)));

            await PublishSchemaEventSender.SendAsync(
                PublishDocumentEvent.Completed(sessionId));

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

            results.MatchSnapshot(matchOptions =>
                matchOptions.Assert(fieldOption =>
                    Assert.Collection(
                        fieldOption.Fields<string>("[*].Data.onPublishSchema.sessionId"),
                        t => Assert.Equal(sessionId, t),
                        t => Assert.Equal(sessionId, t))));
        }
    }
}

