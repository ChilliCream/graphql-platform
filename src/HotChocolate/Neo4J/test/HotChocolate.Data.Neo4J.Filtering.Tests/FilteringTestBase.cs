using System;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class FilteringTestBase
    {
        private Func<IResolverContext, IExecutable<TResult>> BuildResolver<TResult>(
            Neo4jResource mongoResource,
            params TResult[] results)
            where TResult : class =>
            ctx => () => new Neo4JExecutable<TResult>();

        protected IRequestExecutor CreateSchema<TEntity, T>(
            TEntity[] entities,
            Neo4jResource  neo4JResource,
            bool withPaging = false)
            where TEntity : class
            where T : FilterInputType<TEntity>
        {
            Func<IResolverContext, IExecutable<TEntity>> resolver = BuildResolver(
                neo4JResource,
                entities);

            return new ServiceCollection()
                .AddGraphQL()
                .AddFiltering(x => x.BindRuntimeType<TEntity, T>().AddNeo4JDefaults())
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("root")
                        .Resolver(resolver)
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
