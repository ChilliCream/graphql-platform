using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Execution;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Issue2373ReproTests
{
    [Fact]
    public async Task Repro_IntermediaryProjection_Overfetching()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<Issue2373DbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var seedContext = new Issue2373DbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();

            var author = new AuthorEntity { Name = "A", Age = 42 };
            var book = new BookEntity { Name = "B", Author = author };

            seedContext.Authors.Add(author);
            seedContext.Books.Add(book);
            await seedContext.SaveChangesAsync();
        }

        string? sql = null;

        var executor = await new ServiceCollection()
            .AddDbContext<Issue2373DbContext>(o => o.UseSqlite(connection))
            .AddGraphQL()
            .AddProjections()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .AddQueryType<Issue2373Query>(
                d => d
                    .Field(t => t.GetBooks(default!))
                    .Use(
                        next => async ctx =>
                        {
                            await next(ctx);
                            if (ctx.Result is IQueryable<BookDto> queryable)
                            {
                                sql = queryable.ToQueryString();
                                ctx.Result = await queryable.ToListAsync();
                            }
                        })
                    .UseProjection())
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              books {
                name
                author {
                  name
                }
              }
            }
            """);

        Assert.NotNull(sql);
        Assert.DoesNotContain("\"a\".\"Age\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"a\".\"Name\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"a\".\"Id\" IS NOT NULL", sql, StringComparison.Ordinal);

        Assert.Empty(Assert.IsType<OperationResult>(result).Errors);
    }

    public sealed class Issue2373DbContext(DbContextOptions<Issue2373DbContext> options)
        : DbContext(options)
    {
        public DbSet<BookEntity> Books => Set<BookEntity>();

        public DbSet<AuthorEntity> Authors => Set<AuthorEntity>();
    }

    public sealed class BookEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int? AuthorId { get; set; }

        public AuthorEntity? Author { get; set; }
    }

    public sealed class AuthorEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }
    }

    public sealed class BookDto
    {
        public string Name { get; set; } = string.Empty;

        public AuthorDto? Author { get; set; }
    }

    public sealed class Issue2373Query
    {
        public IQueryable<BookDto> GetBooks([Service] Issue2373DbContext dbContext)
            => dbContext.Books.Select(
                book => new BookDto
                {
                    Name = book.Name,
                    Author = book.Author == null
                        ? null
                        : new AuthorDto
                        {
                            Id = book.Author.Id,
                            Name = book.Author.Name,
                            Age = book.Author.Age
                        }
                });
    }

    public sealed class AuthorDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }
    }
}
