using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Processing;
using MarshmallowPie.Storage;
using MarshmallowPie.Storage.FileSystem;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace MarshmallowPie.BackgroundServices
{
    public class PublishSchemaDocumentHandlerTests
        : BackgroundServiceTestBase
    {
        public PublishSchemaDocumentHandlerTests(
            MongoResource mongoResource,
            FileStorageResource fileStorageResource)
            : base(mongoResource, fileStorageResource)
        {
        }

        [Fact]
        public async Task HandleMessage()
        {
            // arrange
            var handler = new PublishSchemaDocumentHandler(
                Storage, SchemaRepository, PublishSchemaEventSender);

            var schema = new Schema("abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);

            var environment = new Environment("abc", "def");
            await EnvironmentRepository.AddEnvironmentAsync(environment);

            var message = new PublishDocumentMessage(
                "ghi", environment.Id, schema.Id, Array.Empty<Tag>());

            IFileContainer fileContainer = await Storage.CreateContainerAsync("ghi");
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
            var list = new List<PublishSchemaEvent>();
            using var cts = new CancellationTokenSource(5000);
            IAsyncEnumerable<PublishSchemaEvent> eventStream =
                await PublishSchemaEventReceiver.SubscribeAsync("ghi", cts.Token);
            await foreach (PublishSchemaEvent eventMessage in
                eventStream.WithCancellation(cts.Token))
            {
                list.Add(eventMessage);
            }
            list.MatchSnapshot();
        }
    }
}
