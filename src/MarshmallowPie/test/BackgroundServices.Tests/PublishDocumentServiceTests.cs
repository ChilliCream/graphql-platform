using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.GraphQL;
using MarshmallowPie.Processing;
using MarshmallowPie.Storage;
using MarshmallowPie.Storage.FileSystem;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace MarshmallowPie.BackgroundServices
{
    public class PublishDocumentServiceTests
        : GraphQLIntegrationTestBase
    {
        public PublishDocumentServiceTests(
            MongoResource mongoResource,
            FileStorageResource fileStorageResource)
            : base(mongoResource, fileStorageResource)
        {
        }

        [Fact]
        public async Task ExecutePublishDocumentServer_With_SchemaFile_Handler()
        {
            // arrange
            string sessionId = await SessionCreator.CreateSessionAsync();

            var handler = new PublishNewSchemaDocumentHandler(
                Storage, SchemaRepository, PublishSchemaEventSender);

            using var service = new PublishDocumentService(
                PublishDocumentMessageReceiver, new
                IPublishDocumentHandler[] { handler });

            var schema = new Schema("abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);

            var environment = new Environment("abc", "def");
            await EnvironmentRepository.AddEnvironmentAsync(environment);

            var message = new PublishDocumentMessage(
                sessionId,
                environment.Id,
                schema.Id,
                "externalId",
                Array.Empty<DocumentInfo>(),
                Array.Empty<Tag>());

            IFileContainer fileContainer = await Storage.CreateContainerAsync(sessionId);
            byte[] buffer = Encoding.UTF8.GetBytes(@"
                    type Query {
                        foo: String
                    }
                ");
            await fileContainer.CreateFileAsync("schema.graphql", buffer, 0, buffer.Length);
            await PublishDocumentMessageSender.SendAsync(message);

            // act
            await service.StartAsync(default);

            var list = new List<PublishDocumentEvent>();
            using var cts = new CancellationTokenSource(5000);
            IAsyncEnumerable<PublishDocumentEvent> eventStream =
                await PublishSchemaEventReceiver.SubscribeAsync(sessionId, cts.Token);
            await foreach (PublishDocumentEvent eventMessage in
                eventStream.WithCancellation(cts.Token))
            {
                list.Add(eventMessage);
            }
            list.MatchSnapshot(matchOption =>
                matchOption.Assert(fieldOption =>
                    Assert.Equal(sessionId, fieldOption.Field<string>("[0].SessionId"))));

            SchemaVersion schemaVersion = SchemaRepository.GetSchemaVersions().Single();
            Assert.Equal(
                "a0409bc380483fb817d51f9f08644309ca9b3b6155fd47f4321844d040c0588c",
                schemaVersion.Hash.Hash);
        }
    }
}
