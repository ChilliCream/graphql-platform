using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Types.Selections
{
    public class SqlServerProvider : IResolverProvider
    {
        private static readonly ILoggerFactory ConsoleLogger =
             LoggerFactory.Create(x => x.AddConsole());

        private static readonly ConcurrentDictionary<object, object> _cache =
            new ConcurrentDictionary<object, object>();

        public (IServiceCollection, Func<IResolverContext, IEnumerable<TResult>>)
            CreateResolver<TResult>(params TResult[] results)
                where TResult : class
        {
            return BuildResolver(results);
        }

        private (IServiceCollection, Func<IResolverContext, IEnumerable<TResult>>)
            BuildResolver<TResult>(params TResult[] results)
                where TResult : class
        {
            var dbContext = new DatabaseContext<TResult>();
            dbContext.Database.EnsureDeleted();
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
            private static int counter = 0;
            private SqliteConnection _connection;

            public DatabaseContext()
            {
            }

            public DbSet<TResult> Data { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                _connection = new SqliteConnection("datasource=:memory:");
                _connection.Open();

                optionsBuilder
                    .UseSqlite(_connection)
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
