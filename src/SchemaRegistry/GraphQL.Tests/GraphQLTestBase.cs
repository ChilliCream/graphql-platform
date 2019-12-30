using System;
using HotChocolate;
using HotChocolate.Execution;
using MarshmallowPie.Repositories;
using MarshmallowPie.Repositories.Mongo;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squadron;
using Xunit;

namespace MarshmallowPie.GraphQL
{
    public class GraphQLTestBase
        : IClassFixture<MongoResource>
    {
        protected GraphQLTestBase(MongoResource mongoResource)
        {
            MongoResource = mongoResource;
            MongoDatabase = mongoResource.CreateDatabase(
                "db_" + Guid.NewGuid().ToString("N"));

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IMongoCollection<Environment>>(
                MongoDatabase.GetCollection<Environment>(nameof(Environment)));

            serviceCollection.AddSingleton<IEnvironmentRepository, EnvironmentRepository>();

            serviceCollection.AddGraphQLSchema(builder =>
                builder
                    .AddSchemaRegistry()
                    .EnableRelaySupport());
            serviceCollection.AddQueryExecutor();
            serviceCollection.AddDataLoader<EnvironmentDataLoader>();

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            EnvironmentRepository = services.GetRequiredService<IEnvironmentRepository>();
            Schema = services.GetRequiredService<ISchema>();
            Executor = services.GetRequiredService<IQueryExecutor>();
        }

        protected ISchema Schema { get; }

        protected IQueryExecutor Executor { get; }

        protected MongoResource MongoResource { get; }

        protected IMongoDatabase MongoDatabase { get; }

        protected IEnvironmentRepository EnvironmentRepository { get; }
    }
}
