using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Projections;

public partial class QueryableProjectionParentRequiresTests
{
    [Fact]
    public async Task ParentRequires_Should_MergeWithClientSelection_When_ClientSelectsRequiredNavigation()
    {
        // arrange
        await using var dbContext = await CreateDbContextAsync();
        var executor = await CreateExecutorAsync(dbContext);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              books {
                author {
                  name
                }
                authorProfileUrl
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ParentRequires_Should_MergeWithClientSelection_When_FieldIsPaged()
    {
        // arrange
        await using var dbContext = await CreateDbContextAsync();
        var executor = await CreateExecutorAsync(dbContext);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              booksCursor {
                nodes {
                  author {
                    name
                  }
                  authorProfileUrl
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ParentRequires_Should_MergeWithClientSelection_When_RequirementSelectsWholeNavigation()
    {
        // arrange
        await using var dbContext = await CreateDbContextAsync();
        var executor = await CreateExecutorAsync(dbContext);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              books {
                author { name }
                authorLabel
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("", "Unfiltered")]
    [InlineData("(where: { rating: { eq: 4 } })", "Filtered")]
    [InlineData("(order: { rating: ASC })", "Sorted")]
    [InlineData("(where: { rating: { gt: 1 } }, order: { rating: ASC })", "FilteredAndSorted")]
    public async Task Projection_Should_MergeNestedSelections_When_ListIsSelectedUnderTwoAliases(
        string arguments,
        string snapshotName)
    {
        // arrange
        await using var dbContext = await CreateDbContextAsync();
        var executor = await CreateExecutorAsync(dbContext);

        // act
        var result = await executor.ExecuteAsync(
            $$"""
            {
              books {
                a: reviews{{arguments}} {
                  text
                  reviewer { name }
                }
                b: reviews{{arguments}} {
                  rating
                  reviewer { id }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create($"{snapshotName}_{TestEnvironment.TargetFramework}")
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Projection_Should_PreserveNestedFiltering_When_FirstAliasSelectsTypename()
    {
        // arrange
        await using var dbContext = await CreateDbContextAsync();
        var executor = await CreateExecutorAsync(dbContext);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              bookContainers {
                a: book { __typename }
                b: book {
                  reviews(where: { rating: { eq: 4 } }) {
                    text
                    reviewer { name }
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "bookContainers": [
                  {
                    "a": {
                      "__typename": "Book"
                    },
                    "b": {
                      "reviews": [
                        {
                          "text": "Great",
                          "reviewer": {
                            "name": "Alice"
                          }
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ParentRequires_Should_ProjectListElementMembers_When_RequirementSelectsListElements()
    {
        // arrange
        await using var dbContext = await CreateDbContextAsync();
        var executor = await CreateExecutorAsync(dbContext, typeof(BookReviewExtensions));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              books {
                averageRating
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ParentRequires_Should_MergeWithClientSelection_When_ClientSelectsRequiredList()
    {
        // arrange
        await using var dbContext = await CreateDbContextAsync();
        var executor = await CreateExecutorAsync(dbContext, typeof(BookReviewExtensions));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              books {
                reviews {
                  text
                }
                averageRating
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ParentRequires_Should_ProjectRequiredMember_When_SchemaIsBuiltWithAddGraphQL()
    {
        // arrange
        Book[] books =
        [
            new()
            {
                Title = "Some Book",
                Author = new Author { Id = 42, Name = "Jane Doe", Biography = "Unselected author biography" }
            }
        ];
        var tester = new SchemaCache().CreateSchema(
            books,
            b => b.Entity<Book>().HasOne(x => x.Author),
            configure: b => b.AddType(typeof(BookExtensions)));

        // act
        var result = await tester.ExecuteAsync(
            """
            {
              root {
                authorProfileUrl
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<DatabaseContext<Book>> CreateDbContextAsync()
    {
        var dbContext = new DatabaseContext<Book>(
            Guid.NewGuid().ToString("N") + ".db",
            b =>
            {
                b.Entity<Book>().HasOne(x => x.Author);
                b.Entity<Book>().HasMany(x => x.Reviews);
                b.Entity<Review>().HasOne(x => x.Reviewer);
            });

        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        dbContext.Data.Add(
            new Book
            {
                Title = "Some Book",
                Author = new Author { Id = 42, Name = "Jane Doe", Biography = "Unselected author biography" },
                Reviews =
                [
                    new Review
                    {
                        Id = 1,
                        Text = "Great",
                        Rating = 4,
                        Notes = "Unselected review notes",
                        Reviewer = new Author { Id = 43, Name = "Alice", Biography = "Unselected reviewer biography" }
                    },
                    new Review
                    {
                        Id = 2,
                        Text = "Fine",
                        Rating = 2,
                        Notes = "Unselected review notes",
                        Reviewer = new Author { Id = 44, Name = "Bob", Biography = "Unselected reviewer biography" }
                    }
                ]
            });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return dbContext;
    }

    private static ValueTask<IRequestExecutor> CreateExecutorAsync(
        DatabaseContext<Book> dbContext,
        params Type[] typeExtensions)
    {
        var builder = new ServiceCollection()
            .AddSingleton(dbContext)
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension(typeof(BookExtensions));

        foreach (var typeExtension in typeExtensions)
        {
            builder.AddTypeExtension(typeExtension);
        }

        return builder
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .ModifyOptions(o => o.ValidatePipelineOrder = false)
            .UseRequest(
                (_, next) => async context =>
                {
                    await next(context);

                    if (context.ContextData.TryGetValue("sql", out var sql))
                    {
                        var result = context.Result.ExpectOperationResult();
                        result.ContextData = result.ContextData.SetItem("sql", sql);
                    }
                })
            .UseDefaultPipeline()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
    }

    public class Query
    {
        [CaptureSql]
        [UseProjection]
        public IQueryable<Book> GetBooks(DatabaseContext<Book> dbContext)
            => dbContext.Data.AsNoTracking();

        [UsePaging]
        [CaptureSql]
        [UseProjection]
        public IQueryable<Book> GetBooksCursor(DatabaseContext<Book> dbContext)
            => dbContext.Data.AsNoTracking();

        [UseProjection]
        public IQueryable<BookContainer> GetBookContainers(DatabaseContext<Book> dbContext)
            => dbContext.Data.AsNoTracking().Select(book => new BookContainer { Book = book });
    }

    public class BookContainer
    {
        public required Book Book { get; set; }
    }

    public class Book
    {
        public int Id { get; set; }

        public required string Title { get; set; }

        public required Author Author { get; set; }

        [UseFiltering]
        [UseSorting]
        public List<Review> Reviews { get; set; } = [];
    }

    public class Author
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public required string Biography { get; set; }
    }

    public class Review
    {
        public int Id { get; set; }

        public required string Text { get; set; }

        public int Rating { get; set; }

        public required string Notes { get; set; }

        public required Author Reviewer { get; set; }
    }

    [ExtendObjectType<Book>]
    public static class BookExtensions
    {
        public static string GetAuthorProfileUrl([Parent(requires: "{ Author { Id } }")] Book book)
            => $"/authors/{book.Author.Id}";

        public static string GetAuthorLabel([Parent(requires: "{ Author }")] Book book)
            => $"{book.Author.Name} ({book.Author.Id})";
    }

    [ExtendObjectType<Book>]
    public static class BookReviewExtensions
    {
        public static double GetAverageRating([Parent(requires: "{ Reviews { Rating } }")] Book book)
            => book.Reviews.Average(t => t.Rating);
    }

    private sealed class CaptureSqlAttribute : ObjectFieldDescriptorAttribute
    {
        public CaptureSqlAttribute([CallerLineNumber] int order = 0)
        {
            Order = order;
        }

        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
            => descriptor.Use(
                next => async context =>
                {
                    await next(context);

                    if (context.Result is IQueryable<Book> queryable)
                    {
                        context.ContextData["sql"] = queryable.ToQueryString();
                    }
                });
    }
}
