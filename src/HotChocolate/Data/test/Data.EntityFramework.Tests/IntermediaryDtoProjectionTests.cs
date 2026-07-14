using System.Data.Common;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class IntermediaryDtoProjectionTests
{
    [Fact]
    public async Task Projection_Should_Not_Overfetch_When_Using_Intermediary_Dto()
    {
        // arrange
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<ProjectionDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var seedContext = new ProjectionDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync(cancellationToken);

            var author = new AuthorEntity { Name = "A", Age = 42 };
            seedContext.Authors.Add(author);
            seedContext.Books.AddRange(
                new BookEntity { Name = "With Author", Author = author },
                new BookEntity { Name = "Without Author" });
            await seedContext.SaveChangesAsync(cancellationToken);
        }

        var sql = new List<string>();

        var executor = await new ServiceCollection()
            .AddDbContext<ProjectionDbContext>(
                o => o
                    .UseSqlite(connection)
                    .AddInterceptors(new SqlCapturingInterceptor(sql)))
            .AddGraphQL()
            .AddProjections()
            .AddQueryType<ProjectionQuery>(
                d => d.Field(t => t.GetBooks(default!)).UseProjection())
            .BuildRequestExecutorAsync(cancellationToken: cancellationToken);

        // act
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
            """,
            cancellationToken);

        // assert
        await new Snapshot()
            .Add(string.Join("\n", sql), "SQL")
            .Add(result, "Result")
            .MatchMarkdownAsync(cancellationToken);
    }

    [Fact]
    public async Task Projection_Should_Preserve_Child_When_Key_Is_Nullable()
    {
        // arrange
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<ProjectionDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var seedContext = new ProjectionDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync(cancellationToken);

            seedContext.Books.Add(
                new BookEntity
                {
                    Name = "Book",
                    Author = new AuthorEntity { Name = "Author", Age = 42 }
                });
            await seedContext.SaveChangesAsync(cancellationToken);
        }

        var sql = new List<string>();

        var executor = await new ServiceCollection()
            .AddDbContext<ProjectionDbContext>(
                o => o
                    .UseSqlite(connection)
                    .AddInterceptors(new SqlCapturingInterceptor(sql)))
            .AddGraphQL()
            .AddProjections()
            .AddQueryType<ProjectionQuery>(
                d => d
                    .Field(t => t.GetBooksWithNullableAuthorId(default!))
                    .UseProjection())
            .BuildRequestExecutorAsync(cancellationToken: cancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              booksWithNullableAuthorId {
                name
                author {
                  name
                }
              }
            }
            """,
            cancellationToken);

        // assert
        await new Snapshot()
            .Add(string.Join("\n", sql), "SQL")
            .Add(result, "Result")
            .MatchMarkdownAsync(cancellationToken);
    }

    private sealed class SqlCapturingInterceptor(List<string> queries) : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            queries.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            queries.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    public sealed class ProjectionDbContext(DbContextOptions<ProjectionDbContext> options)
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

    public sealed class ProjectionQuery
    {
        public IQueryable<BookDto> GetBooks([Service] ProjectionDbContext dbContext)
            => dbContext.Books.OrderBy(book => book.Id).Select(
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

        public IQueryable<BookWithNullableAuthorIdDto> GetBooksWithNullableAuthorId(
            [Service] ProjectionDbContext dbContext)
            => dbContext.Books.OrderBy(book => book.Id).Select(
                book => new BookWithNullableAuthorIdDto
                {
                    Name = book.Name,
                    Author = book.Author == null
                        ? null
                        : new AuthorWithNullableIdDto
                        {
                            Id = null,
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

    public sealed class BookWithNullableAuthorIdDto
    {
        public string Name { get; set; } = string.Empty;

        public AuthorWithNullableIdDto? Author { get; set; }
    }

    public sealed class AuthorWithNullableIdDto
    {
        public int? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }
    }
}
