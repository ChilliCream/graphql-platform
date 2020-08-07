using System;
using System.Collections.Generic;
using System.Threading;
using Castle.Core.Logging;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Squadron;

namespace HotChocolate.Data.Filters.Expressions
{
    public class DatabaseContext<T> : DbContext
        where T : class
    {
        private static int _dbCounter = 0;

        private readonly SqlServerResource _resource;

        public DatabaseContext(SqlServerResource resource)
        {
            _resource = resource;
        }

        public DbSet<T> Data { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var id = Interlocked.Increment(ref _dbCounter);
            var connectionString = _resource.CreateConnectionString("database_" + id);
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    public class FilterVisitorTestBase
    {
        private readonly object _lock = new object();

        protected SqlServerResource? Resource { get; set; }

        public FilterVisitorTestBase(SqlServerResource resource)
        {
            Init(resource);
        }


        public FilterVisitorTestBase()
        {
        }

        public virtual void Init(SqlServerResource resource)
        {
            if (Resource == null)
            {
                lock (_lock)
                {
                    if (Resource == null)
                    {
                        Resource = resource;
                    }
                }
            }
        }

        private Func<IResolverContext, IEnumerable<TResult>> BuildResolver<TResult>(
            params TResult[] results)
            where TResult : class
        {
            if (Resource == null)
            {
                throw new InvalidOperationException();
            }

            var dbContext = new DatabaseContext<TResult>(Resource);
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            dbContext.AddRange(results);
            dbContext.SaveChanges();

            return ctx => dbContext.Data.AsQueryable();
        }

        protected T[] CreateEntity<T>(params T[] entities) => entities;

        protected IRequestExecutor CreateSchema<TEntity, T>(
            TEntity[] entities,
            FilterConvention? convention = null)
            where TEntity : class
            where T : IFilterInputType
        {
            convention ??= new FilterConvention(x => x.UseDefault());

            Func<IResolverContext, IEnumerable<TEntity>>? resolver = BuildResolver(entities);

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<IFilterConvention>(convention)
                .UseFiltering()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("root")
                        .Resolver(resolver)
                        .UseFiltering<T>());

            ISchema? schema = builder.Create();

            return schema.MakeExecutable();
        }

    }
}

