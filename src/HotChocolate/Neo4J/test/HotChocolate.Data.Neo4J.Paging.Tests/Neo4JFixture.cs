using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Execution;
using HotChocolate.Types;
using Neo4j.Driver;
using Squadron;

namespace HotChocolate.Data.Neo4J.Paging;

public class Neo4JFixture : Neo4jResource<Neo4JConfig>
{
    private readonly ConcurrentDictionary<(Type, string), Task<IRequestExecutor>> _cache =
        new();

    public Task<IRequestExecutor> GetOrCreateSchema<T>(string cypher)
        where T : class
    {
        (Type, string) key = (typeof(T), cypher);

        return _cache.GetOrAdd(
            key,
            k => CreateSchema<T>(cypher));
    }

    private async Task<IRequestExecutor> CreateSchema<TEntity>(string cypher)
        where TEntity : class
    {
        var session = GetAsyncSession();
        var cursor = await session.RunAsync(cypher);
        await cursor.ConsumeAsync();

        return await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("root")
                    .Resolve(new Neo4JExecutable<TEntity>(GetAsyncSession()))
                    .Use(
                        next => async context =>
                        {
                            await next(context);
                            if (context.Result is IExecutable executable)
                            {
                                context.ContextData["query"] = executable.Print();
                            }
                        })
                    .UseOffsetPaging<ObjectType<TEntity>>())
            .AddNeo4JPagingProviders()
            .UseRequest(
                next => async context =>
                {
                    await next(context);
                    if (context.ContextData.TryGetValue("query", out var queryString))
                    {
                        context.Result =
                            QueryResultBuilder
                                .FromResult(context.Result!.ExpectQueryResult())
                                .SetContextData("query", queryString)
                                .Create();
                    }
                })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
    }
}