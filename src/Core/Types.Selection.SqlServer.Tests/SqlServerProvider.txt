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
    public class SqlServerProvider : IResolverProvider
    {
        private static readonly ILoggerFactory ConsoleLogger =
             LoggerFactory.Create(x => x.AddConsole());

        private static readonly ConcurrentDictionary<object, object> _cache =
            new ConcurrentDictionary<object, object>();

        private readonly SqlServerResource _resource;

        public SqlServerProvider(SqlServerResource resource)
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
            var dbContext = new DatabaseContext<TResult>(
             _resource.CreateDatabaseAsync(
                     $"CREATE DATABASE useSelection",
                     $"useSelection")
                 .GetAwaiter()
                 .GetResult());
            dbContext.Database.EnsureCreated();
            dbContext.Data.AddRange(results);
            dbContext.SaveChanges();

            var services = new ServiceCollection();
            services.AddSingleton(dbContext);

            return (services, ctx => ctx.Service<DatabaseContext<TResult>>().Data.AsQueryable());
        }

        private class DatabaseContext<TResult> : DbContext
            where TResult : class
        {
            private readonly string _connectionString;

            public DatabaseContext(string connectionString)
            {
                _connectionString = connectionString;
            }

            public DbSet<TResult> Data { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder
                    .UseSqlServer(_connectionString)
                    .EnableSensitiveDataLogging()
                    .UseLoggerFactory(ConsoleLogger);
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<SelectionTestsBase.Foo>()
                    .Ignore(x => x.ObjectArray);
            }
        }

        public class SelectionAttributeTestsBaseFooNested
        {
            public Guid FooId { get; set; }
            public SelectionAttributeTestsBase.Foo Foo { get; set; }
            public Guid NestedFooId { get; set; }
            public SelectionAttributeTestsBase.NestedFoo NestedFoo { get; set; }
        }
    }
}
