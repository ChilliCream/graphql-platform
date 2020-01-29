using System;
using MarshmallowPie.Processing;
using MarshmallowPie.Processing.InMemory;
using MarshmallowPie.Repositories;
using MarshmallowPie.Repositories.Mongo;
using MarshmallowPie.Storage;
using MarshmallowPie.Storage.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squadron;
using Xunit;

namespace MarshmallowPie.BackgroundServices
{
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
