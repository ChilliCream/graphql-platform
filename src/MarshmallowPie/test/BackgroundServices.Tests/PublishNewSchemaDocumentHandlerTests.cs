using System;
using System.Collections.Generic;
using System.IO;
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
    public class PublishNewSchemaDocumentHandlerTests
        : GraphQLIntegrationTestBase
    {
        public PublishNewSchemaDocumentHandlerTests(
            MongoResource mongoResource,
            FileStorageResource fileStorageResource)
            : base(mongoResource, fileStorageResource)
        {
        }

        [Fact]
        public async Task HandleMessage()
        {
            // arrange
            string sessionId = await SessionCreator.CreateSessionAsync();

            var handler = new PublishNewSchemaDocumentHandler(
                Storage, SchemaRepository, PublishSchemaEventSender);

            var schema = new Schema("abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);

            var environment = new Environment("abc", "def");
            await EnvironmentRepository.AddEnvironmentAsync(environment);

            var message = new PublishDocumentMessage(
                sessionId, environment.Id, schema.Id, "externalId", Array.Empty<Tag>());

            IFileContainer fileContainer = await Storage.CreateContainerAsync(sessionId);
            using (Stream stream = await fileContainer.CreateFileAsync("schema.graphql"))
            {
                byte[] buffer = Encoding.UTF8.GetBytes(@"
                    type Query {
                        foo: String
                    }
                ");
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }

            // act
            await handler.HandleAsync(message, default);

            // assert
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
        }
    }
}
