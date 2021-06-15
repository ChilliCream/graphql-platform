using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Squadron;

namespace HotChocolate.Data.Neo4J.Projections
{
    public class Neo4JFixture : Neo4jResource<Neo4JConfig>
    {
        private readonly ConcurrentDictionary<(Type, object), Task<IRequestExecutor>>
            _cache = new();

        public Task<IRequestExecutor> GetOrCreateSchema<T>(string cypher)
            where T : class
        {
            (Type, string) key = (typeof(T), cypher);

            return _cache.GetOrAdd(
                key,
                k => CreateSchema<T>(cypher));
        }

        protected async Task<IRequestExecutor> CreateSchema<TEntity>(string cypher)
            where TEntity : class
        {
            IAsyncSession session = GetAsyncSession();
            IResultCursor cursor = await session.RunAsync(cypher);
            await cursor.ConsumeAsync();

            IRequestExecutorBuilder builder = new ServiceCollection().AddGraphQL();

            return builder
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
                                    .Resolver(new Neo4JExecutable<TEntity>(GetAsyncSession())));
                        }))
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
                .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
                .UseDefaultPipeline()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync()
                .Result;
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

        public class StubObject<T>
        {
            public T Root { get; set; }
        }
    }
}
