using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Types.Filters
{
    public class SqlServerProvider
    {
        private static readonly ILoggerFactory ConsoleLogger =
             LoggerFactory.Create(x => x.AddConsole());

        public (IServiceCollection, Func<IResolverContext, IEnumerable<TResult>>)
            CreateResolver<TResult>(params TResult[] results)
                where TResult : class
        {
            return BuildResolverInternal(results);
        }

        public (IServiceCollection, Func<IResolverContext, IEnumerable<TResult>>)
            CreateResolverFromList<TResult>(IEnumerable<TResult> results)
                where TResult : class
        {
            return BuildResolverInternal(results);
        }

        private (IServiceCollection, Func<IResolverContext, IEnumerable<TResult>>)
            BuildResolverInternal<TResult>(IEnumerable<TResult> results)
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
                base.OnModelCreating(modelBuilder);
            }
        }

    }
}
