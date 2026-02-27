using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Projections;

public class Issue6329ReproTests
{
    private readonly SchemaCache _cache = new();

    private readonly BookWithProjectedAuthor[] _books =
    [
        new()
        {
            Id = 1,
            Title = "Book",
            Author = new AuthorWithProjectedId
            {
                Id = 10,
                Name = "Author"
            }
        }
    ];

    [Fact]
    public async Task IsProjected_SubObject_Can_Be_Omitted_From_Selection()
    {
        var tester = _cache.CreateSchema(
            _books,
            onModelCreating: modelBuilder =>
                modelBuilder.Entity<BookWithProjectedAuthor>().OwnsOne(x => x.Author),
            usePaging: true);

        var result = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                      root {
                        edges {
                          node {
                            id
                          }
                        }
                      }
                    }
                    """)
                .Build());

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
    }

    public class BookWithProjectedAuthor
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        [IsProjected(true)]
        public AuthorWithProjectedId Author { get; set; } = new();
    }

    [Owned]
    public class AuthorWithProjectedId
    {
        [IsProjected(true)]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
