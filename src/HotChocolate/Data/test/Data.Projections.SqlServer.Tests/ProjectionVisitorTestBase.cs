using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Data.Projections.Expressions;
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

        private Func<IResolverContext, IQueryable<TResult>> BuildResolver<TResult>(
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
            ProjectionProvider? provider = null,
            Action<ModelBuilder>? onModelCreating = null,
            bool usePaging = false,
            bool useOffsetPaging = false,
            INamedType? objectType = null,
            Action<ISchemaBuilder>? configure = null)
            where TEntity : class
        {
            provider ??= new QueryableProjectionProvider(x => x.AddDefaults());
            var convention = new ProjectionConvention(x => x.Provider(provider));

            Func<IResolverContext, IQueryable<TEntity>> resolver =
                BuildResolver(onModelCreating, entities);

            ISchemaBuilder builder = SchemaBuilder.New();

            if (objectType is { })
            {
                builder.AddType(objectType);
            }

            configure?.Invoke(builder);

            builder
                .AddConvention<IProjectionConvention>(convention)
                .AddProjections()
                .AddFiltering()
                .AddSorting()
                .AddQueryType(
                    new ObjectType<StubObject<TEntity>>(
                        c =>
                        {
                            c.Name("Query");

                            ApplyConfigurationToFieldDescriptor<TEntity>(
                                c.Field(x => x.Root).Resolver(resolver),
                                usePaging,
                                useOffsetPaging);

                            ApplyConfigurationToFieldDescriptor<TEntity>(
                                c.Field("rootExecutable")
                                    .Resolver(ctx => resolver(ctx).AsExecutable()),
                                usePaging,
                                useOffsetPaging);
                        }));

            ISchema schema = builder.Create();

            return new ServiceCollection()
                .Configure<RequestExecutorSetup>(Schema.DefaultName, o => o.Schema = schema)
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

        private static void ApplyConfigurationToFieldDescriptor<TEntity>(
            IObjectFieldDescriptor descriptor,
            bool usePaging = false,
            bool useOffsetPaging = false)
        {
            if (usePaging)
            {
                descriptor.UsePaging<ObjectType<TEntity>>();
            }

            if (useOffsetPaging)
            {
                descriptor.UseOffsetPaging<ObjectType<TEntity>>();
            }

            descriptor
                .Use(
                    next => async context =>
                    {
                        await next(context);

                        if (context.Result is IQueryable<TEntity> queryable)
                        {
                            try
                            {
                                context.ContextData["sql"] = queryable.ToQueryString();
                            }
                            catch (Exception ex)
                            {
                                context.ContextData["sql"] = ex.Message;
                            }

                            context.Result = await queryable.ToListAsync();
                        }

                        if (context.Result is IExecutable executable)
                        {
                            try
                            {
                                context.ContextData["sql"] = executable.Print();
                            }
                            catch (Exception ex)
                            {
                                context.ContextData["sql"] = ex.Message;
                            }
                        }
                    })
                .UseFiltering()
                .UseSorting()
                .UseProjection();
        }

        public class StubObject<T>
        {
            public T Root { get; set; }
        }
    }
}
