using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Processing;
using MarshmallowPie.Processing.InMemory;
using MarshmallowPie.Repositories;
using MarshmallowPie.Repositories.Mongo;
using MarshmallowPie.Storage;
using MarshmallowPie.Storage.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace MarshmallowPie.BackgroundServices
{
    public class PublishSchemaDocumentHandlerTests
        : BackgroundServiceTestBase
    {
        protected PublishSchemaDocumentHandlerTests(
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
            await foreach (PublishSchemaEvent eventMessage in eventStream.WithCancellation(cts.Token))
            {
                list.Add(eventMessage);
            }
            list.MatchSnapshot();
        }

    }

    public class BackgroundServiceTestBase
        : IClassFixture<MongoResource>
        , IClassFixture<FileStorageResource>
    {
        protected BackgroundServiceTestBase(
            MongoResource mongoResource,
            FileStorageResource fileStorageResource)
        {
            MongoResource = mongoResource;
            MongoDatabase = mongoResource.CreateDatabase(
                "db_" + Guid.NewGuid().ToString("N"));

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddMongoRepositories(sp => MongoDatabase);

            serviceCollection.AddSingleton(new MessageQueue<PublishDocumentMessage>());
            serviceCollection.AddSingleton(new SessionMessageQueue<PublishSchemaEvent>());

            serviceCollection.AddSingleton<IMessageSender<PublishDocumentMessage>>(sp =>
                sp.GetRequiredService<MessageQueue<PublishDocumentMessage>>());
            serviceCollection.AddSingleton<IMessageSender<PublishSchemaEvent>>(sp =>
                sp.GetRequiredService<SessionMessageQueue<PublishSchemaEvent>>());

            serviceCollection.AddSingleton<IMessageReceiver<PublishDocumentMessage>>(sp =>
                sp.GetRequiredService<MessageQueue<PublishDocumentMessage>>());
            serviceCollection.AddSingleton<ISessionMessageReceiver<PublishSchemaEvent>>(sp =>
                sp.GetRequiredService<SessionMessageQueue<PublishSchemaEvent>>());

            serviceCollection.AddSingleton(fileStorageResource.CreateStorage());

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            EnvironmentRepository = services.GetRequiredService<IEnvironmentRepository>();
            SchemaRepository = services.GetRequiredService<ISchemaRepository>();
            Storage = services.GetRequiredService<IFileStorage>();
            PublishDocumentMessageSender = services.GetRequiredService<IMessageSender<PublishDocumentMessage>>();
            PublishSchemaEventSender = services.GetRequiredService<IMessageSender<PublishSchemaEvent>>();
            PublishDocumentMessageReceiver = services.GetRequiredService<IMessageReceiver<PublishDocumentMessage>>();
            PublishSchemaEventReceiver = services.GetRequiredService<ISessionMessageReceiver<PublishSchemaEvent>>();
        }

        protected MongoResource MongoResource { get; }

        protected IMongoDatabase MongoDatabase { get; }

        protected IEnvironmentRepository EnvironmentRepository { get; }

        protected ISchemaRepository SchemaRepository { get; }

        protected IFileStorage Storage { get; }

        protected IMessageSender<PublishDocumentMessage> PublishDocumentMessageSender { get; }

        protected IMessageSender<PublishSchemaEvent> PublishSchemaEventSender { get; }

        protected IMessageReceiver<PublishDocumentMessage> PublishDocumentMessageReceiver { get; }

        protected ISessionMessageReceiver<PublishSchemaEvent> PublishSchemaEventReceiver { get; }

    }
}
