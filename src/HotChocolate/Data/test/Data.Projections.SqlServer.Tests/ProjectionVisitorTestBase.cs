using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Projections
{
    public class ProjectionVisitorTestBase
    {
        protected string? FileName { get; set; } = Guid.NewGuid().ToString("N") + ".db";

        private Func<IResolverContext, IEnumerable<TResult>> BuildResolver<TResult>(
            Action<ModelBuilder>? onModelCreating = null,
            params TResult[] results)
            where TResult : class
        {
            if (FileName is null)
            {
                throw new InvalidOperationException();
            }

            var dbContext = new DatabaseContext<TResult>(FileName, onModelCreating);
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            dbContext.AddRange(results);

            try
            {
                dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
            }

            return ctx => dbContext.Data.AsQueryable();
        }

        protected T[] CreateEntity<T>(params T[] entities) => entities;

        public IRequestExecutor CreateSchema<TEntity>(
            TEntity[] entities,
            ProjectionConvention? convention = null,
            Action<ModelBuilder>? onModelCreating = null,
            bool usePaging = false)
            where TEntity : class
        {
            convention ??= new QueryableProjectionConvention(
                x => x.AddDefaults());

            Func<IResolverContext, IEnumerable<TEntity>> resolver = BuildResolver(
                onModelCreating,
                entities);

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<IProjectionConvention>(convention)
                .AddProjections()
                .AddFiltering()
                .AddSorting()
                .AddQueryType(
                    new ObjectType<StubObject<TEntity>>(
                        c =>
                        {
                            IObjectFieldDescriptor descriptor = c
                                .Name("Query")
                                .Field(x => x.Root)
                                .Resolver(resolver)
                                .Use(
                                    next => async context =>
                                    {
                                        await next(context);

                                        if (context.Result is IQueryable<TEntity> queryable)
                                        {
                                            try
                                            {
                                                context.ContextData["sql"] =
                                                    queryable.ToQueryString();
                                            }
                                            catch (Exception)
                                            {
                                                context.ContextData["sql"] =
                                                    "EF Core 3.1 does not support ToQuerString offically";
                                            }

                                            context.Result = await queryable.ToListAsync();
                                        }
                                    })
                                .UseFiltering()
                                .UseSorting()
                                .UseProjection();

                            if (usePaging)
                            {
                                descriptor.UsePaging<ObjectType<TEntity>>();
                            }
                        }));

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
                            context.ContextData.TryGetValue("sql", out object? queryString))
                        {
                            context.Result =
                                QueryResultBuilder
                                    .FromResult(result)
                                    .SetContextData("sql", queryString)
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

        public class StubObject<T>
        {
            public T Root { get; set; }
        }
    }
}
