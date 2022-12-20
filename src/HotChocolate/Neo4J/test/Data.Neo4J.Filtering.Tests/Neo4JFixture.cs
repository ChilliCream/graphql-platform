using System.Collections.Concurrent;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Neo4J.Filtering;

public class Neo4JFixture : Neo4JFixtureBase
{
    private readonly ConcurrentDictionary<(Type, string), Task<IRequestExecutor>> _cache = new();

    public async Task<IRequestExecutor> Arrange<TEntity, TFilter>(
        Neo4JDatabase database,
        string cypher)
        where TEntity : class
        where TFilter : FilterInputType
    {
        await ResetDatabase(database, cypher);

        return await GetOrCreateSchema<TEntity, TFilter>(database, cypher);
    }

    private Task<IRequestExecutor> GetOrCreateSchema<TEntity, TFilter>(
        Neo4JDatabase database,
        string cypher)
        where TEntity : class
        where TFilter : FilterInputType
    {
        (Type, string) key = (typeof(TEntity), cypher);

        return _cache.GetOrAdd(
            key,
            k => CreateSchema<TEntity, TFilter>(database));
    }

    private static async Task<IRequestExecutor> CreateSchema<TEntity, TFilter>(
        Neo4JDatabase database)
        where TEntity : class
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddNeo4JProjections()
            .AddFiltering(x =>
                x.BindRuntimeType(typeof(TEntity), typeof(TFilter)).AddNeo4JDefaults())
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("root")
                    .SetupNeo4JTestField<TEntity>(database.GetAsyncSession)
                    .UseFiltering<TFilter>()
                    .UseProjection())
            .SetupNeo4JTestResponse()
            .UseDefaultPipeline()
            .ModifyOptions(o => o.ValidatePipelineOrder = false)
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
    }
}
