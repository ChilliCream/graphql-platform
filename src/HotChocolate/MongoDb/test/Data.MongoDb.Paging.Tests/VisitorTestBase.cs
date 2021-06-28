using System;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters
{
    public class FilterVisitorTestBase
    {
        protected string? FileName { get; set; } = Guid.NewGuid().ToString("N") + ".db";

        private Func<IResolverContext, IExecutable<TResult>> BuildResolver<TResult>(
            MongoResource mongoResource,
            params TResult[] results)
            where TResult : class
        {
            if (FileName is null)
            {
                throw new InvalidOperationException();
            }

            IMongoCollection<TResult> collection =
                mongoResource.CreateCollection<TResult>("data_" + Guid.NewGuid().ToString("N"));

            collection.InsertMany(results);

            return ctx => collection.AsExecutable();
        }

        protected IRequestExecutor CreateSchema<TEntity, T>(
            TEntity[] entities,
            MongoResource mongoResource,
            bool withPaging = false)
            where TEntity : class
            where T : FilterInputType<TEntity>
        {
            Func<IResolverContext, IExecutable<TEntity>> resolver = BuildResolver(
                mongoResource,
                entities);

            return new ServiceCollection()
                .AddGraphQL()
                .AddFiltering(x => x.BindRuntimeType<TEntity, T>().AddMongoDbDefaults())
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
