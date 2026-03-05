using HotChocolate.Data.Filters;
using HotChocolate.Data.NodaTime.TestContext;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Extensions;
using Squadron;

namespace HotChocolate.Data.NodaTime;

[Collection(PostgresCollectionFixture.DefinitionName)]
public sealed class IntegrationTests(PostgreSqlResource resource)
{
    private string CreateConnectionString()
        => resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task NodaTime_Paging_Filtering_And_Sorting()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new BookContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType()
            .AddTypeExtension(typeof(Query))
            .AddType<Types.NodaTime.LocalDateType>()
            // We bind DateOnly to the NodaTime scalar so that HotChocolate does not try to bind it
            // to its default scalar of the same name.
            .BindRuntimeType<DateOnly, Types.NodaTime.LocalDateType>()
            // Add type converters so that DateOnly can be translated to LocalDate and vice versa.
            .AddTypeConverter<DateOnly, LocalDate>(d => d.ToLocalDate())
            .AddTypeConverter<LocalDate, DateOnly>(d => d.ToDateOnly())
            .AddFiltering(
                c =>
                    c.AddDefaults()
                        .BindRuntimeType<LocalDate, LocalDateOperationFilterInputType>()
                        // We bind DateOnly here to the LocalDateOperationFilterInputType so that
                        // HotChocolate can use the same filter for both types.
                        .BindRuntimeType<DateOnly, LocalDateOperationFilterInputType>())
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    books(
                        where: { publishedDate: { gt: "2008-01-15", lt: "2008-01-18" } }
                        order: { publishedDate: DESC }) {
                        nodes {
                            title
                            publishedDate
                        }
                    }
                }
                """));

        // assert
        result.ExpectOperationResult().MatchInlineSnapshot(
            """
            {
                "data": {
                  "books": {
                    "nodes": [
                    {
                        "title": "Book2",
                        "publishedDate": "2008-01-17"
                    },
                    {
                        "title": "Book1",
                        "publishedDate": "2008-01-16"
                    }
                    ]
                  }
                }
            }
            """);
    }

    private static async Task SeedAsync(string connectionString)
    {
        await using var context = new BookContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        for (var i = 1; i <= 10; i++)
        {
            var book = new Book
            {
                Id = i,
                Title = $"Book{i}",
                Author = new Author
                {
                    Id = i,
                    Name = $"Author{i}",
                    BirthDate = new DateOnly(1974, 6, 19)
                },
                PublishedDate = new LocalDate(2008, 1, 15 + i)
            };

            context.Books.Add(book);
        }

        await context.SaveChangesAsync();
    }

    [QueryType]
    private static class Query
    {
        [UsePaging]
        [UseFiltering]
        [UseSorting]
        public static IQueryable<Book> GetBooks(BookContext bookContext) => bookContext.Books;
    }
}

public sealed class LocalDateOperationFilterInputType
    : ComparableOperationFilterInputType<LocalDate>;
