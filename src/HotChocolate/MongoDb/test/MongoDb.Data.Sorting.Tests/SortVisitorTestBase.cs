using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Data;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.MongoDb.Sorting.Convention.Extensions;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Squadron;

namespace HotChocolate.MongoDb.Data.Sorting
{
    public class SortVisitorTestBase
    {
        protected string? FileName { get; } = Guid.NewGuid().ToString("N") + ".db";

        private Func<IResolverContext, Task<IEnumerable<TResult>>> BuildResolver<TResult>(
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

            return async ctx =>
            {
                if (ctx.LocalContextData.TryGetValue(
                        nameof(SortDefinition<TResult>),
                        out object? def) &&
                    def is BsonDocument filter)
                {
                    ctx.ContextData["query"] = filter.ToString();
                    List<TResult> result = await (await collection.FindAsync(
                        FilterDefinition<TResult>.Empty,
                        new FindOptions<TResult> { Sort = filter })).ToListAsync();
                    return result;
                }

                return new List<TResult>();
            };
        }

        protected IRequestExecutor CreateSchema<TEntity, T>(
            TEntity[] entities,
            MongoResource mongoResource,
            bool withPaging = false)
            where TEntity : class
            where T : SortInputType<TEntity>
        {
            Func<IResolverContext, Task<IEnumerable<TEntity>>> resolver = BuildResolver(
                mongoResource,
                entities);

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddSorting(x => x.BindRuntimeType<TEntity, T>().AddMongoDbDefaults())
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("root")
                        .Resolver(resolver)
                        .UseSorting<T>());

            ISchema schema = builder.Create();

            return new ServiceCollection()
                .Configure<RequestExecutorFactoryOptions>(
                    Schema.DefaultName,
                    o => o.Schema = schema)
                .AddGraphQL()
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
                .Result;
        }
    }
}
