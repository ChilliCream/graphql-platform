using System.Collections.Concurrent;
using HotChocolate.Data.Neo4J.Sorting;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Neo4JFixture : Neo4JFixtureBase
{
    private readonly ConcurrentDictionary<(Type, object), Task<IRequestExecutor>>
        _cache = new();

    public async Task<IRequestExecutor> Arrange<TEntity, TType>(Neo4JDatabase database, string cypher)
        where TEntity : class
        where TType : SortInputType<TEntity>
    {
        await ResetDatabase(database, cypher);

        return await GetOrCreateSchema<TEntity, TType>(database);
    }

    private Task<IRequestExecutor> GetOrCreateSchema<T, TType>(Neo4JDatabase database)
        where T : class
        where TType : SortInputType<T>
    {
        var key = (typeof(T), typeof(TType));
        return _cache.GetOrAdd(
            key,
            k => CreateSchema<T, TType>(database));
    }

    private static async Task<IRequestExecutor> CreateSchema<TEntity, T>(Neo4JDatabase database)
        where TEntity : class
        where T : SortInputType<TEntity>
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddSorting(x => x.BindRuntimeType<TEntity, T>().AddNeo4JDefaults())
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("root")
                    .SetupNeo4JTestField<TEntity>(database.GetAsyncSession)
                    .UseSorting<T>())
            .SetupNeo4JTestResponse()
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
    }
}
