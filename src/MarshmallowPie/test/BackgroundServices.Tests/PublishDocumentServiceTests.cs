using System;
using System.Collections.Generic;
using System.IO;
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
            var handler = new PublishSchemaDocumentHandler(
                Storage, SchemaRepository, PublishSchemaEventSender);

            using var service = new PublishDocumentService(
                PublishDocumentMessageReceiver, new
                IPublishDocumentHandler[] { handler });

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

            await PublishDocumentMessageSender.SendAsync(message);

            // act
            await service.StartAsync(default);

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

            SchemaVersion schemaVersion = SchemaRepository.GetSchemaVersions().Single();
            Assert.Equal("oECbw4BIP7gX1R+fCGRDCcqbO2FV/Uf0MhhE0EDAWIw=", schemaVersion.Hash);
        }
    }
}
