using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using HotChocolate.Execution;
using MarshmallowPie.Processing;
using MarshmallowPie.Repositories;
using MarshmallowPie.Repositories.Mongo;
using MarshmallowPie.Storage;
using MarshmallowPie.Storage.FileSystem;
using MongoDB.Driver;
using Squadron;
using Xunit;

namespace MarshmallowPie.GraphQL
{
    public class GraphQLIntegrationTestBase
        : IClassFixture<MongoResource>
        , IClassFixture<FileStorageResource>
    {
        protected GraphQLIntegrationTestBase(
            MongoResource mongoResource,
            FileStorageResource fileStorageResource)
        {
            MongoResource = mongoResource;
            MongoDatabase = mongoResource.CreateDatabase(
                "db_" + Guid.NewGuid().ToString("N"));

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddMongoRepositories(sp => MongoDatabase);
            serviceCollection.AddInMemoryMessageQueue();
            serviceCollection.AddSingleton(fileStorageResource.CreateStorage());

            serviceCollection.AddGraphQLSchema(builder =>
                builder
                    .AddSchemaRegistry()
                    .EnableRelaySupport());

            serviceCollection.AddQueryExecutor();
            serviceCollection.AddSchemaRegistryDataLoader();
            serviceCollection.AddSchemRegistryErrorFilters();

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            Schema = services.GetRequiredService<ISchema>();
            Executor = services.GetRequiredService<IQueryExecutor>();
            EnvironmentRepository = services.GetRequiredService<IEnvironmentRepository>();
            SchemaRepository = services.GetRequiredService<ISchemaRepository>();
            Storage = services.GetRequiredService<IFileStorage>();
            SessionCreator = services.GetRequiredService<ISessionCreator>();
            PublishDocumentMessageSender =
                services.GetRequiredService<IMessageSender<PublishDocumentMessage>>();
            PublishSchemaEventSender =
                services.GetRequiredService<IMessageSender<PublishDocumentEvent>>();
            PublishDocumentMessageReceiver =
                services.GetRequiredService<IMessageReceiver<PublishDocumentMessage>>();
            PublishSchemaEventReceiver =
                services.GetRequiredService<ISessionMessageReceiver<PublishDocumentEvent>>();
        }

        protected ISchema Schema { get; }

        protected IQueryExecutor Executor { get; }

        protected MongoResource MongoResource { get; }

        protected IMongoDatabase MongoDatabase { get; }

        protected IEnvironmentRepository EnvironmentRepository { get; }

        protected ISchemaRepository SchemaRepository { get; }

        protected IFileStorage Storage { get; }

        protected ISessionCreator SessionCreator { get; }

        protected IMessageSender<PublishDocumentMessage> PublishDocumentMessageSender { get; }

        protected IMessageSender<PublishDocumentEvent> PublishSchemaEventSender { get; }

        protected IMessageReceiver<PublishDocumentMessage> PublishDocumentMessageReceiver { get; }

        protected ISessionMessageReceiver<PublishDocumentEvent> PublishSchemaEventReceiver { get; }
    }
}
