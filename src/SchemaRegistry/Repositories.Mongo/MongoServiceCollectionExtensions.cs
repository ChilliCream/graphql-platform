using System;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MarshmallowPie.Repositories.Mongo
{
    public static class MongoServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoRepositories(
            this IServiceCollection services,
            Func<IServiceProvider, IMongoDatabase> getMongoDatabase)
        {
            return services
                .AddSingleton<IMongoCollection<Environment>>(sp =>
                    getMongoDatabase(sp).GetCollection<Environment>(nameof(Environment)))
                .AddSingleton<IMongoCollection<Schema>>(sp =>
                    getMongoDatabase(sp).GetCollection<Schema>(nameof(Schema)))
                .AddSingleton<IMongoCollection<SchemaVersion>>(sp =>
                    getMongoDatabase(sp).GetCollection<SchemaVersion>(nameof(SchemaVersion)))
                .AddSingleton<IEnvironmentRepository, EnvironmentRepository>()
                .AddSingleton<ISchemaRepository, SchemaRepository>();
        }
    }
}
