using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task Sorting_Should_Work_When_UsedWithNonNullDateTime()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddSorting()
            .BuildRequestExecutorAsync();

        const string query = @"
        {
            foos(order: { createdUtc: DESC }) {
                createdUtc
            }
        }
        ";

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Sorting_Should_Work_When_Nested()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddSorting()
            .BuildRequestExecutorAsync();

        const string query = @"
        {
            books(order: [{ author: { name: ASC } }]) {
                title
                author {
                    name
                }
            }
        }
        ";

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchSnapshot();
    }
}

public class Query
{
    [UseSorting]
    public IEnumerable<Foo> Foos() =>
    [
        new Foo { CreatedUtc = new DateTime(2000, 1, 1, 1, 1, 1) },
        new Foo { CreatedUtc = new DateTime(2010, 1, 1, 1, 1, 1) },
        new Foo { CreatedUtc = new DateTime(2020, 1, 1, 1, 1, 1) }
    ];

    [UseSorting]
    public IEnumerable<Book> GetBooks(QueryContext<Book> queryContext)
        => new[]
            {
                new Book { Title = "Book5", Author = new Author { Name = "Author6" } },
                new Book { Title = "Book7", Author = new Author { Name = "Author17" } },
                new Book { Title = "Book1", Author = new Author { Name = "Author5" } }
            }
            .AsQueryable()
            .With(queryContext);
}

public class Foo
{
    [GraphQLType(typeof(NonNullType<DateType>))]
    public DateTime CreatedUtc { get; set; }
}

public class Author
{
    public string Name { get; set; } = string.Empty;

    [UseSorting]
    public Book[] Books { get; set; } = [];
}

public class Book
{
    public string Title { get; set; } = string.Empty;
    public Author? Author { get; set; }
}
