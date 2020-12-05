using System;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squadron;

namespace HotChocolate.Data.MongoDb.Projections
{
    public class ProjectionVisitorTestBase
    {
        private Func<IResolverContext, IExecutable<TResult>> BuildResolver<TResult>(
            MongoResource mongoResource,
            params TResult[] results)
            where TResult : class
        {
            IMongoCollection<TResult> collection =
                mongoResource.CreateCollection<TResult>("data_" + Guid.NewGuid().ToString("N"));

            collection.InsertMany(results);

            return ctx => collection.AsExecutable();
        }

        protected T[] CreateEntity<T>(params T[] entities) => entities;

        public IRequestExecutor CreateSchema<TEntity>(
            TEntity[] entities,
            MongoResource mongoResource,
            bool usePaging = false,
            bool useOffsetPaging = false,
            ObjectType<TEntity>? objectType = null)
            where TEntity : class
        {
            Func<IResolverContext, IExecutable<TEntity>> resolver = BuildResolver(
                mongoResource,
                entities);

            var builder = new ServiceCollection().AddGraphQL();

            if (objectType is {})
            {
                builder.AddType(objectType);
            }

            return builder
                .AddMongoDbProjections()
                .AddMongoDbFiltering()
                .AddMongoDbSorting()
                .AddQueryType(
                    new ObjectType<StubObject<TEntity>>(
                        c =>
                        {
                            c.Name("Query");
                            ApplyConfigurationToFieldDescriptor<TEntity>(
                                c.Field(x => x.Root).Resolver(resolver),
                                usePaging,
                                useOffsetPaging);
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

        private static void ApplyConfigurationToFieldDescriptor<TEntity>(
            IObjectFieldDescriptor descriptor,
            bool usePaging = false,
            bool useOffsetPaging = false)
        {
            if (usePaging)
            {
                descriptor.UseMongoPaging<ObjectType<TEntity>>();
            }

            if (useOffsetPaging)
            {
                descriptor.UseMongoOffsetPaging<ObjectType<TEntity>>();
            }

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
