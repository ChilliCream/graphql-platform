using Microsoft.Extensions.DependencyInjection;

namespace MarshmallowPie.Repositories.Mongo
{
    public static class MongoServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoRepositories(this IServiceCollection services)
        {
            return services
                .AddSingleton<IEnvironmentRepository, EnvironmentRepository>()
                .AddSingleton<ISchemaRepository, SchemaRepository>();
        }
    }
}
