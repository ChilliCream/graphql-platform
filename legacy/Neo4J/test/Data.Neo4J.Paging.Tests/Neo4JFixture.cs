using System.Collections.Concurrent;
using HotChocolate.Data.Neo4J;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Neo4JFixture : Neo4JFixtureBase
{
    private readonly ConcurrentDictionary<(Type, string), Task<IRequestExecutor>> _cache =
        new();

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
            k => CreateSchema<T>(database));
    }

    private static async Task<IRequestExecutor> CreateSchema<TEntity>(Neo4JDatabase database)
        where TEntity : class
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("root")
                    .SetupNeo4JTestField<TEntity>(database.GetAsyncSession)
                    .UseOffsetPaging<ObjectType<TEntity>>())
            .AddNeo4JPagingProviders()
            .SetupNeo4JTestResponse()
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
    }
}
