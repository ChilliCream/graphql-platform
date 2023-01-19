using System.Linq;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class IntegrationTests : IClassFixture<AuthorFixture>
{
    private readonly DbSet<Author> _authors;
    private readonly DbSet<SingleOrDefaultAuthor> _singleOrDefaultAuthors;
    private readonly DbSet<ZeroAuthor> _zeroAuthors;

    public IntegrationTests(AuthorFixture authorFixture)
    {
        _authors = authorFixture.Context.Authors;
        _zeroAuthors = authorFixture.Context.ZeroAuthors;
        _singleOrDefaultAuthors = authorFixture.Context.SingleOrDefaultAuthors;
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnAllItems_When_ToListAsync()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Resolve(_authors.AsExecutable()))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_OnlyOneItem_When_SingleOrDefault()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<Author>>()
                    .Resolve(_authors.Take(1).AsExecutable())
                    .UseSingleOrDefault())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_SingleOrDefaultMoreThanOne()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<Author>>()
                    .Resolve(_authors.AsExecutable())
                    .UseSingleOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnNull_When_SingleOrDefaultZero()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<Author>>()
                    .Resolve(_authors.Take(0).AsExecutable())
                    .UseSingleOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<Author>>()
                    .Resolve(_authors.AsExecutable())
                    .UseFirstOrDefault())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnNull_When_FirstOrDefaultZero()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<ZeroAuthor>>()
                    .Resolve(_zeroAuthors.Take(0).AsExecutable())
                    .UseFirstOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task OffsetPagingExecutable()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddPooledDbContextFactory<BookContext>(
                b => b.UseInMemoryDatabase("Data Source=EF.OffsetPagingExecutable.db"))
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // assert
        var result = await executor.ExecuteAsync(
            @"query Test {
                authorOffsetPagingExecutable {
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
    public async Task ExecuteAsync_Should_ReturnAllItems_When_ToListAsync_AsyncEnumerable()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Resolve(new QueryableExecutable<Author>(_authors))
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_OnlyOneItem_When_SingleOrDefault_AsyncEnumerable()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<SingleOrDefaultAuthor>>()
                    .Resolve(
                        new QueryableExecutable<SingleOrDefaultAuthor>(_singleOrDefaultAuthors))
                    .UseSingleOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_SingleOrDefaultMoreThanOne_AsyncEnumerable()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<Author>>()
                    .Resolve(new QueryableExecutable<Author>(_authors))
                    .UseSingleOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnNull_When_SingleOrDefaultZero_AsyncEnumerable()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<ZeroAuthor>>()
                    .Resolve(new QueryableExecutable<ZeroAuthor>(_zeroAuthors))
                    .UseSingleOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault_AsyncEnumerable()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<Author>>()
                    .Resolve(new QueryableExecutable<Author>(_authors))
                    .UseFirstOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnNull_When_FirstOrDefaultZero_AsyncEnumerable()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<ZeroAuthor>>()
                    .Resolve(new QueryableExecutable<ZeroAuthor>(_zeroAuthors))
                    .UseFirstOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }
}
