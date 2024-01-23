using System;
using System.Linq;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class UseDbContextTests
{
    [Fact]
    public async Task Execute_Queryable()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync("{ authors { name } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_Task()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<QueryTask>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync("{ authors { name } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_ValueTask()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<QueryValueTask>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync("{ authors { name } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_OffsetPaging_TotalCount()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
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
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_OffsetPaging_TotalCount_Task()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<QueryTask>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
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
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_OffsetPaging_TotalCount_QueryableExtensions()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
            @"query Test {
                    queryableExtensionsOffset {
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
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_OffsetPaging_TotalCount_ValueTask()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<QueryValueTask>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
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
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_OffsetPaging()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
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
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_OffsetPaging_Task()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<QueryTask>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
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
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_OffsetPaging_ValueTask()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<QueryValueTask>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
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
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Single()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync("{ author { name } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task DbContextType_Is_Object()
    {
        // arrange
        // act
        async Task CreateSchema() =>
            await new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<InvalidQuery>()
                .BuildSchemaAsync();

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(CreateSchema);
        exception.Errors.First().Message.MatchSnapshot();
    }

    [Fact]
    public async Task Infer_Schema_From_IQueryable_Fields()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<Query>()
                .BuildSchemaAsync();

        // assert
        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Infer_Schema_From_IQueryable_Task_Fields()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<QueryTask>()
                .BuildSchemaAsync();

        // assert
        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Infer_Schema_From_IQueryable_ValueTask_Fields()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<QueryValueTask>()
                .BuildSchemaAsync();

        // assert
        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task DbContext_ResolverExtension()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"query Test {
                    books {
                        id
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task DbContext_ResolverExtension_Missing_DbContext()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true)
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"query Test {
                    booksWithMissingContext {
                        id
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_CursorPaging_TotalCount()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
            @"query Test {
                    authorCursorPaging {
                        nodes {
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
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_CursorPaging_TotalCount_Task()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<QueryTask>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
            @"query Test {
                    authorCursorPaging {
                        nodes {
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
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_CursorPaging_TotalCount_QueryableExtensions()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
            @"query Test {
                    queryableExtensionsCursor {
                        nodes {
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
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_CursorPaging_TotalCount_ValueTask()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<QueryValueTask>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
            @"query Test {
                    authorCursorPaging {
                        nodes {
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
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_CursorPaging()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
            @"query Test {
                    authorCursorPaging {
                        nodes {
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_CursorPaging_Task()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
            @"query Test {
                    authorCursorPaging {
                        nodes {
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Queryable_CursorPaging_ValueTask()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddPooledDbContextFactory<BookContext>(
                    b => b.UseInMemoryDatabase(CreateConnectionString()))
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider();

        var executor =
            await services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

        var contextFactory =
            services.GetRequiredService<IDbContextFactory<BookContext>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Authors.AddAsync(new Author { Name = "foo", });
            await context.Authors.AddAsync(new Author { Name = "bar", });
            await context.SaveChangesAsync();
        }

        // act
        var result = await executor.ExecuteAsync(
            @"query Test {
                    authorCursorPaging {
                        nodes {
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    private static string CreateConnectionString() =>
        $"Data Source={Guid.NewGuid():N}.db";
}
