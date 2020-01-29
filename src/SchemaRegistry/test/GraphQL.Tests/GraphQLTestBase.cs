using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using MarshmallowPie.Processing;
using MarshmallowPie.Repositories;
using MarshmallowPie.Repositories.Mongo;
using MarshmallowPie.Storage;
using MarshmallowPie.Storage.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Squadron;
using Xunit;

namespace MarshmallowPie.GraphQL
{
    public class GraphQLTestBase
        : IClassFixture<MongoResource>
        , IClassFixture<FileStorageResource>
    {
        private readonly List<object> _receivedMessages = new List<object>();

        protected GraphQLTestBase(MongoResource mongoResource, FileStorageResource fileStorageResource)
        {
            MongoResource = mongoResource;
            MongoDatabase = mongoResource.CreateDatabase(
                "db_" + Guid.NewGuid().ToString("N"));

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddMongoRepositories(sp => MongoDatabase);

            serviceCollection.AddGraphQLSchema(builder =>
                builder
                    .AddSchemaRegistry()
                    .EnableRelaySupport());
            serviceCollection.AddQueryExecutor();
            serviceCollection.AddSchemaRegistryDataLoader();
            serviceCollection.AddSchemRegistryErrorFilters();

            var publishDocumentMessageSender = new Mock<IMessageSender<PublishDocumentMessage>>();
            publishDocumentMessageSender.Setup(t => t.SendAsync(
                It.IsAny<PublishDocumentMessage>(),
                It.IsAny<CancellationToken>()))
                .Returns(new Func<PublishDocumentMessage, CancellationToken, Task>((m, c) =>
                {
                    _receivedMessages.Add(m);
                    return Task.CompletedTask;
                }));
            serviceCollection.AddSingleton(publishDocumentMessageSender.Object);
            serviceCollection.AddSingleton(fileStorageResource.CreateStorage());

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            EnvironmentRepository = services.GetRequiredService<IEnvironmentRepository>();
            SchemaRepository = services.GetRequiredService<ISchemaRepository>();
            Schema = services.GetRequiredService<ISchema>();
            Executor = services.GetRequiredService<IQueryExecutor>();
            Storage = services.GetRequiredService<IFileStorage>();
            ReceivedMessages = _receivedMessages;
        }

        protected ISchema Schema { get; }

        protected IQueryExecutor Executor { get; }

        protected MongoResource MongoResource { get; }

        protected IMongoDatabase MongoDatabase { get; }

        protected IEnvironmentRepository EnvironmentRepository { get; }

        protected ISchemaRepository SchemaRepository { get; }

        protected IFileStorage Storage { get; }

        protected IReadOnlyList<object> ReceivedMessages { get; }
    }
}
