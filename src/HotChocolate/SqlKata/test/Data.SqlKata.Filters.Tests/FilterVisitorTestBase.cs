using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace HotChocolate.Data.SqlKata.Filters
{
    public class FilterVisitorTestBase
    {
        protected string? FileName { get; set; } = Guid.NewGuid().ToString("N") + ".db";

        private Func<IResolverContext, IExecutable<TResult>> BuildResolver<TResult>(
            Action<ModelBuilder>? onModelCreating,
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

            SqlKataExecutable<TResult> executable = new();
            return ctx =>
            {
                executable.WithQueryFactory(ctx.Services.GetRequiredService<QueryFactory>());
                return executable;
            };
        }

        protected T[] CreateEntity<T>(params T[] entities) => entities;

        protected IRequestExecutor CreateSchema<TEntity, T>(
            TEntity[] entities,
            FilterConvention? convention = null,
            bool withPaging = false,
            Action<ISchemaBuilder>? configure = null,
            Action<ModelBuilder>? onModelCreating = null)
            where TEntity : class
            where T : FilterInputType<TEntity>
        {
            convention ??=
                new FilterConvention(x => x.AddSqlKataDefaults().BindRuntimeType<TEntity, T>());

            Func<IResolverContext, IExecutable<TEntity>>? resolver =
                BuildResolver(onModelCreating, entities);

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<IFilterConvention>(convention)
                .AddSqlKataFiltering()
                .AddQueryType(
                    c =>
                    {
                        ApplyConfigurationToField<TEntity, T>(
                            c.Name("Query").Field("root").Resolver(resolver),
                            withPaging);

                        ApplyConfigurationToField<TEntity, T>(
                            c.Name("Query").Field("rootExecutable").Resolver(ctx => resolver(ctx)),
                            withPaging);
                    });

            configure?.Invoke(builder);

            ISchema schema = builder.Create();

            return new ServiceCollection()
                .AddSingleton(_ =>
                {
                    // In real life you may read the configuration dynamically
                    var connection = new SqliteConnection($"Data Source={FileName}");
                    var compiler = new SqliteCompiler();
                    return new QueryFactory(connection, compiler);
                })
                .Configure<RequestExecutorSetup>(Schema.DefaultName, o => o.Schema = schema)
                .AddGraphQL()
                .UseRequest(
                    next => async context =>
                    {
                        await next(context);
                        if (context.Result is IReadOnlyQueryResult result &&
                            context.ContextData.TryGetValue("sql", out var queryString))
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

        private void ApplyConfigurationToField<TEntity, TType>(
            IObjectFieldDescriptor field,
            bool withPaging)
            where TEntity : class
            where TType : FilterInputType<TEntity>
        {
            field.Use(
                next => async context =>
                {
                    await next(context);

                    if (context.Result is IExecutable queryable)
                    {
                        context.ContextData["sql"] = queryable.Print();
                    }
                });

            if (withPaging)
            {
                field.UsePaging<ObjectType<TEntity>>();
            }

            field.UseFiltering<TType>();
        }
    }
}
