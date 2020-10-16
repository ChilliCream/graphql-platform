using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

namespace Neo4jDemo.Extensions
{
    public static class ObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UseSession<TSession>(
            this IObjectFieldDescriptor descriptor)
            where TSession : IAsyncSession
        {
            return descriptor.UseScopedService(
                create: s => s.GetRequiredService<IDriver>().AsyncSession(o => o.WithDatabase("neo4j")),
                dispose: (s, c) => c.CloseAsync());
        }
    }
}
