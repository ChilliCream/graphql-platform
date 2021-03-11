using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public static class Neo4JObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UseAsyncSessionWithDatabase(
            this IObjectFieldDescriptor descriptor,
            string dbName)
        {
            return descriptor.UseScopedService(
                create: s =>
                    s.GetRequiredService<IDriver>()
                        .AsyncSession(o => o.WithDatabase(dbName)),
                dispose: null);
        }

        public static IObjectFieldDescriptor UseNeo4JRepository(
            this IObjectFieldDescriptor descriptor)
        {
            return descriptor.UseScopedService(
                create: s =>
                    s.GetRequiredService<Neo4JRepository>()
                        ,
                dispose: null);
        }
    }
}
