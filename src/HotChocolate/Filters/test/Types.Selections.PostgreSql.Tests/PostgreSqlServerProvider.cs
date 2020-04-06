using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Squadron;

namespace HotChocolate.Types.Selections
{
    public class PostgreSqlServerProvider
    {
        private static readonly ILoggerFactory ConsoleLogger =
             LoggerFactory.Create(x => x.AddConsole());

        private static readonly ConcurrentDictionary<object, object> _cache =
            new ConcurrentDictionary<object, object>();

        private readonly PostgreSqlResource _resource;

        public PostgreSqlServerProvider(PostgreSqlResource resource)
        {
            _resource = resource;
        }

        public (IServiceCollection, Func<IResolverContext, IEnumerable<TResult>>)
            CreateResolver<TResult>(params TResult[] results)
                where TResult : class
        {
            if (_cache.GetOrAdd(results, (obj) => BuildResolver(results))
                    is ValueTuple<IServiceCollection,
                        Func<IResolverContext, IEnumerable<TResult>>> result)
            {
                return result;
            }
            throw new InvalidOperationException("Cache is in invalid state!");
        }

        private (IServiceCollection, Func<IResolverContext, IEnumerable<TResult>>)
            BuildResolver<TResult>(params TResult[] results)
                where TResult : class
        {
            var dbContext = new DatabaseContext<TResult>(_resource);
            dbContext.Database.EnsureCreated();
            dbContext.AddRange(results);
            dbContext.SaveChanges();

            var services = new ServiceCollection();
            services.AddSingleton(dbContext);

            return (services, ctx => ctx.Service<DatabaseContext<TResult>>().Data.AsQueryable());
        }

        private class DatabaseContext<TResult> : DbContext
            where TResult : class
        {
            private static int increment = 0;
            private readonly PostgreSqlResource _resource;

            public DatabaseContext(PostgreSqlResource resource)
            {
                _resource = resource;
            }

            public DbSet<TResult> Data { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                _resource.CreateDatabaseAsync(typeof(TResult).Name + ++increment)
                    .GetAwaiter().GetResult();

                optionsBuilder
                    .UseNpgsql(_resource.GetConnection(typeof(TResult).Name + increment))
                    .EnableSensitiveDataLogging()
                    .UseLoggerFactory(ConsoleLogger);
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<SelectionTests.Foo>().HasOne(x => x.NestedNull);
                modelBuilder.Entity<SelectionTests.Foo>().HasOne(x => x.Nested);
                modelBuilder.Entity<SelectionTests.Foo>().HasMany(x => x.ObjectList);
                modelBuilder.Entity<SelectionTests.Foo>().Ignore(x => x.ObjectArray);
                modelBuilder.Entity<SelectionTests.NestedFoo>().HasMany(x => x.ObjectArray);
                modelBuilder.Entity<SelectionTests.NestedFoo>().HasOne(x => x.NestedNull);
                modelBuilder.Entity<SelectionTests.NestedFoo>().HasOne(x => x.Nested);
                base.OnModelCreating(modelBuilder);
            }
        }
    }

    public class SelectionAttributeTestsFooNested
    {
        public Guid FooId { get; set; }
        public SelectionAttributeTests.Foo Foo { get; set; }
        public Guid NestedFooId { get; set; }
        public SelectionAttributeTests.NestedFoo NestedFoo { get; set; }
    }
}
