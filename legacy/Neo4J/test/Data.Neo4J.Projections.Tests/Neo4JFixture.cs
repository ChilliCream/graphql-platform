using System.Collections.Concurrent;
using HotChocolate.Data.Neo4J;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Neo4JFixture : Neo4JFixtureBase
{
    private readonly ConcurrentDictionary<(Type, object), Task<IRequestExecutor>> _cache = new();

    public async Task<IRequestExecutor> Arrange<TEntity>(Neo4JDatabase database, string cypher)
        where TEntity : class
    {
        await ResetDatabase(database, cypher);

        return await GetOrCreateSchema<TEntity>(database, cypher);
    }

    private Task<IRequestExecutor> GetOrCreateSchema<T>(Neo4JDatabase database, string cypher)
        where T : class
    {
        (Type, string) key = (typeof(T), cypher);

        return _cache.GetOrAdd(
            key,
            _ => CreateSchema<T>(database));
    }

    private static async Task<IRequestExecutor> CreateSchema<TEntity>(Neo4JDatabase database)
        where TEntity : class
    {
        var builder = new ServiceCollection().AddGraphQL();

        return await builder
            .AddNeo4JProjections()
            .AddNeo4JFiltering()
            .AddNeo4JSorting()
            .AddQueryType(
                new ObjectType<StubObject<TEntity>>(
                    c =>
                    {
                        c.Name("Query");
                        ApplyConfigurationToFieldDescriptor(
                            c.Field(x => x.Root)
                                .SetupNeo4JTestField<TEntity>(database.GetAsyncSession));
                    }))
            .SetupNeo4JTestResponse()
            .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
    }

    private static void ApplyConfigurationToFieldDescriptor(IObjectFieldDescriptor descriptor)
    {
        descriptor
            .Use(
                next => async context =>
                {
                    await next(context);
                    if (context.Result is IExecutable executable)
                    {
                        context.ContextData["query"] = executable.Print();
                    }
                })
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }

    private class StubObject<T>
    {
        public T Root { get; set; } = default!;
    }
}
