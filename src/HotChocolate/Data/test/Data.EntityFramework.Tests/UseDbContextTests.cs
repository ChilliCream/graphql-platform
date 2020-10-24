using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data
{
    public class UseDbContextTests
    {
        [Fact]
        public async Task Execute_Queryable()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddPooledDbContextFactory<BookContext>(
                        b => b.UseInMemoryDatabase("Data Source=books.db"))
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IDbContextFactory<BookContext> contextFactory =
                services.GetRequiredService<IDbContextFactory<BookContext>>();

            await using (BookContext context = contextFactory.CreateDbContext())
            {
                await context.Authors.AddAsync(new Author { Name = "foo" });
                await context.SaveChangesAsync();
            }

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ authors { name } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Queryable_OffsetPaging_TotalCount()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddPooledDbContextFactory<BookContext>(
                        b => b.UseInMemoryDatabase("Data Source=books.db"))
                    .AddGraphQL()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .AddQueryType<Query>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IDbContextFactory<BookContext> contextFactory =
                services.GetRequiredService<IDbContextFactory<BookContext>>();

            await using (BookContext context = contextFactory.CreateDbContext())
            {
                await context.Authors.AddAsync(new Author { Name = "foo" });
                await context.Authors.AddAsync(new Author { Name = "bar" });
                await context.SaveChangesAsync();
            }

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"query Test {
                    authorOffsetPaging {
                        items {
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Queryable_OffsetPaging()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddPooledDbContextFactory<BookContext>(
                        b => b.UseInMemoryDatabase("Data Source=books.db"))
                    .AddGraphQL()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .AddQueryType<Query>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IDbContextFactory<BookContext> contextFactory =
                services.GetRequiredService<IDbContextFactory<BookContext>>();

            await using (BookContext context = contextFactory.CreateDbContext())
            {
                await context.Authors.AddAsync(new Author { Name = "foo" });
                await context.Authors.AddAsync(new Author { Name = "bar" });
                await context.SaveChangesAsync();
            }

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"query Test {
                    authorOffsetPaging {
                        items {
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Single()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddPooledDbContextFactory<BookContext>(
                        b => b.UseInMemoryDatabase("Data Source=books.db"))
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IDbContextFactory<BookContext> contextFactory =
                services.GetRequiredService<IDbContextFactory<BookContext>>();

            await using (BookContext context = contextFactory.CreateDbContext())
            {
                await context.Authors.AddAsync(new Author { Name = "foo" });
                await context.SaveChangesAsync();
            }

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ author { name } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task DbContextType_Is_Object()
        {
            // arrange
            // act
            async Task CreateSchema() =>
                await new ServiceCollection()
                    .AddPooledDbContextFactory<BookContext>(
                        b => b.UseInMemoryDatabase("Data Source=books.db"))
                    .AddGraphQL()
                    .AddQueryType<InvalidQuery>()
                    .BuildSchemaAsync();

            // assert
            SchemaException exception = await Assert.ThrowsAsync<SchemaException>(CreateSchema);
            exception.Errors.First().Message.MatchSnapshot();
        }
    }
}
