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
            where T : SortInputType<TEntity>
        {
            Func<IResolverContext, IExecutable<TEntity>> resolver = BuildResolver(
                mongoResource,
                entities);

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddSorting(x => x.BindRuntimeType<TEntity, T>().AddMongoDbDefaults())
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
