using System;
using MarshmallowPie.Repositories;
using MarshmallowPie.Repositories.Mongo;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MarshmallowPie
{
    public static class MongoServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoRepositories(
            this IServiceCollection services,
            Func<IServiceProvider, IMongoDatabase> getMongoDatabase)
        {
            return services
                .AddMongoCollection<Environment>(getMongoDatabase)
                .AddMongoCollection<Schema>(getMongoDatabase)
                .AddMongoCollection<SchemaVersion>(getMongoDatabase)
                .AddMongoCollection<SchemaPublishReport>(getMongoDatabase)
                .AddMongoCollection<PublishedSchema>(getMongoDatabase)
                .AddMongoCollection<Client>(getMongoDatabase)
                .AddMongoCollection<ClientVersion>(getMongoDatabase)
                .AddMongoCollection<ClientPublishReport>(getMongoDatabase)
                .AddMongoCollection<PublishedClient>(getMongoDatabase)
                .AddMongoCollection<QueryDocument>(getMongoDatabase)
                .AddSingleton<IEnvironmentRepository, EnvironmentRepository>()
                .AddSingleton<ISchemaRepository, SchemaRepository>()
                .AddSingleton<IClientRepository, ClientRepository>();
        }

        private static IServiceCollection AddMongoCollection<T>(
            this IServiceCollection services,
            Func<IServiceProvider, IMongoDatabase> getMongoDatabase)
        {
            return services.AddSingleton<IMongoCollection<T>>(sp =>
                getMongoDatabase(sp).GetCollection<T>(typeof(T).Name));
        }
    }
}
