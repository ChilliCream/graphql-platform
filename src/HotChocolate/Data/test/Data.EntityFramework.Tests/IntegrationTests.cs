using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Data.Extensions;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data;

public class IntegrationTests : IClassFixture<AuthorFixture>
{
    private readonly DbSet<Author> _authors;
    private readonly DbSet<SingleOrDefaultAuthor> _singleOrDefaultAuthors;
    private readonly DbSet<ZeroAuthor> _zeroAuthors;
    private readonly DbSet<Book> _books;
    private readonly DbSet<BookNoAuthor> _bookNoAuthors;

    public IntegrationTests(AuthorFixture authorFixture)
    {
        _authors = authorFixture.Context.Authors;
        _zeroAuthors = authorFixture.Context.ZeroAuthors;
        _singleOrDefaultAuthors = authorFixture.Context.SingleOrDefaultAuthors;
        _books = authorFixture.Context.Books;
        _bookNoAuthors = authorFixture.Context.BookNoAuthors;
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnAllItems_When_ToListAsync()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
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
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_OnlyOneItem_When_SingleOrDefault()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
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
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_SingleOrDefaultMoreThanOne()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
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
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnNull_When_SingleOrDefaultZero()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
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
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
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
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnNull_When_FirstOrDefaultZero()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
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
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task OffsetPagingExecutable()
    {
        // arrange
        // act
        IRequestExecutor executor = await new ServiceCollection()
            .AddPooledDbContextFactory<BookContext>(
                b => b.UseInMemoryDatabase("Data Source=EF.OffsetPagingExecutable.db"))
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // assert
        IExecutionResult result = await executor.ExecuteAsync(
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
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnAllItems_When_ToListAsync_AsyncEnumerable()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
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
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_OnlyOneItem_When_SingleOrDefault_AsyncEnumerable()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
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
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_SingleOrDefaultMoreThanOne_AsyncEnumerable()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
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
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnNull_When_SingleOrDefaultZero_AsyncEnumerable()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
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
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault_AsyncEnumerable()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
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
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnNull_When_FirstOrDefaultZero_AsyncEnumerable()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
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
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProjectCorrectly_When_Select()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddEntityFrameworkProjections()
            .AddType<AuthorDto.Type>()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("test")
                    .Type<ObjectType<BookDto>>()
                    .Resolve(_ => _books.AsQueryable()
                        .Select(book =>
                            new BookDto
                            {
                                Title = book.Title,
                                Author = new AuthorDto
                                {
                                    Id = book.Author!.Id,
                                    Name = book.Author.Name
                                }
                            })
                    )
                    .UseFirstOrDefault()
                    .UseSqlLogging()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .UseSqlLogging()
            .BuildRequestExecutorAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    test {
                        title
                        author {
                            id
                        }
                    }
                }
                ");

        // assert
        result.MatchSqlSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProjectCorrectly_When_SelectButNoAuthor()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddEntityFrameworkProjections()
            .AddType<AuthorDto.Type>()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("test")
                    .Type<ObjectType<BookDto>>()
                    .Resolve(_ => _bookNoAuthors.AsQueryable()
                        .Select(book =>
                            new BookDto
                            {
                                Title = book.Title,
                                Author = book.Author != null
                                    ? new AuthorDto
                                    {
                                        Id = book.Author.Id,
                                        Name = book.Author.Name
                                    }
                                    : null
                            })
                    )
                    .UseFirstOrDefault()
                    .UseSqlLogging()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .UseSqlLogging()
            .BuildRequestExecutorAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            @"
                {
                    test {
                        title
                        author {
                            id
                        }
                    }
                }
                ");

        // assert
        result.MatchSqlSnapshot();
    }

    public class BookDto
    {
        public int Id { get; set; }

        public int AuthorId { get; set; }

        public string? Title { get; set; }

        public virtual AuthorDto? Author { get; set; }
    }

    public class AuthorDto
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public virtual ICollection<BookDto> Books { get; set; } =
            new List<BookDto>();

        internal class Type : ObjectType<AuthorDto>
        {
            protected override void Configure(IObjectTypeDescriptor<AuthorDto> descriptor)
            {
                descriptor.HasKey(x => x.Id);
            }
        }
    }
}
