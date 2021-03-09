using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Squadron;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class FilteringTestBase
    {
        protected async Task<IRequestExecutor> CreateSchema<TEntity, T>(
            Neo4jResource neo4JResource,
            string query,
            bool withPaging = false)
            where TEntity : class
            where T : FilterInputType<TEntity>
        {
            IAsyncSession session = neo4JResource.GetAsyncSession();
            IResultCursor cursor = await session.RunAsync(query);
            await cursor.ConsumeAsync();

            return new ServiceCollection()
                .AddGraphQL()
                .AddFiltering(x => x.BindRuntimeType<TEntity, T>().AddNeo4JDefaults())
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("root")
                        .Resolver(new Neo4JExecutable<TEntity>(neo4JResource.GetAsyncSession()))
                        .Use(
                            next => async context =>
                            {
                                await next(context);
                                if (context.Result is IExecutable executable)
                                {
                                    context.ContextData["query"] = executable.Print();
                                }
                            })
                        .UseFiltering<T>())
                .UseRequest(
                    next => async context =>
                    {
                        await next(context);
                        if (context.Result is IReadOnlyQueryResult result &&
                            context.ContextData.TryGetValue("query", out object? queryString))
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
