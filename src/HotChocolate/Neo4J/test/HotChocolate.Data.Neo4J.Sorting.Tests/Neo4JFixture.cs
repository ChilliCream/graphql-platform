using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Squadron;

namespace HotChocolate.Data.Neo4J.Sorting
{
    public class Neo4JFixture : Neo4jResource<Neo4JConfig>
    {
        private readonly ConcurrentDictionary<(Type, object), Task<IRequestExecutor>>
            _cache = new();

        public Task<IRequestExecutor> GetOrCreateSchema<T, TType>(string cypher)
            where T : class
            where TType : SortInputType<T>
        {
            (Type, Type) key = (typeof(T), typeof(TType));
            return _cache.GetOrAdd(
                key,
                k => CreateSchema<T, TType>(cypher));
        }

        public async Task<IRequestExecutor> CreateSchema<TEntity, T>(string cypher)
            where TEntity : class
            where T : SortInputType<TEntity>
        {
            IAsyncSession session = GetAsyncSession();
            IResultCursor cursor = await session.RunAsync(cypher);
            await cursor.ConsumeAsync();

            return new ServiceCollection()
                .AddGraphQL()
                .AddSorting(x => x.BindRuntimeType<TEntity, T>().AddNeo4JDefaults())
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("root")
                        .Resolver(new Neo4JExecutable<TEntity>(GetAsyncSession()))
                        .Use(
                            next => async context =>
                            {
                                await next(context);
                                if (context.Result is IExecutable executable)
                                {
                                    context.ContextData["query"] = executable.Print();
                                }
                            })
                        .UseSorting<T>())
                .UseRequest(
                    next => async context =>
                    {
                        await next(context);
                        if (context.Result is IReadOnlyQueryResult result &&
                            context.ContextData.TryGetValue("query", out var queryString))
                        {
                            context.Result =
                                QueryResultBuilder
                                    .FromResult(result)
                                    .SetContextData("query", queryString)
                                    .Create();
                        }
                    })
                .UseDefaultPipeline()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync()
                .GetAwaiter()
                .GetResult();
        }
    }
}
