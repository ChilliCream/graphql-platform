using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Sorting;

public sealed class QueryableSortVisitorProjectionTests
{
    // An OnAfterSortingApplied callback can append ThenBy to the sorted query when the
    // resolver returns a Select projection.
    [Fact]
    public async Task Sort_Should_ApplyPostSortingThenBy_When_ResolverProjectsWithSelect()
    {
        // arrange
        var executor = await CreateExecutorAsync<PostSortingQuery>();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                items(order: [{ name: ASC }]) {
                    id
                    name
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "id": 3,
                    "name": "Alpha"
                  },
                  {
                    "id": 1,
                    "name": "Beta"
                  },
                  {
                    "id": 2,
                    "name": "Beta"
                  }
                ]
              }
            }
            """);
    }

    // Multiple sort fields on a Select projection are applied in order, including a sort field
    // defined by a computed expression rather than a projected member.
    [Fact]
    public async Task Sort_Should_ApplyAllSortFields_When_LaterFieldIsComputedExpression()
    {
        // arrange
        var executor = await CreateExecutorAsync<ExpressionFieldQuery>();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                items(order: [{ name: ASC }, { reverseId: ASC }]) {
                    id
                    name
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "id": 3,
                    "name": "Alpha"
                  },
                  {
                    "id": 2,
                    "name": "Beta"
                  },
                  {
                    "id": 1,
                    "name": "Beta"
                  }
                ]
              }
            }
            """);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync<TQuery>()
        where TQuery : class
    {
        var databaseName = $"{Guid.NewGuid():N}.db";

        return await new ServiceCollection()
            .AddDbContext<ItemContext>(b => b.UseSqlite($"Data Source={databaseName}"))
            .AddGraphQL()
            .AddSorting()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .AddQueryType<TQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
    }

    private static IQueryable<ItemDto> ProjectItems(ItemContext context)
    {
        context.Database.EnsureCreated();

        if (!context.Items.Any())
        {
            context.Items.AddRange(
                new ItemEntity { Id = 1, Name = "Beta" },
                new ItemEntity { Id = 2, Name = "Beta" },
                new ItemEntity { Id = 3, Name = "Alpha" });
            context.SaveChanges();
        }

        return context.Items.Select(x => new ItemDto { Id = x.Id, Name = x.Name });
    }

    public sealed class PostSortingQuery
    {
        [UseSorting]
        public IQueryable<ItemDto> GetItems(ItemContext context, ISortingContext sorting)
        {
            sorting.Handled(false);
            sorting.OnAfterSortingApplied<IQueryable<ItemDto>>(
                static (sortingApplied, query) =>
                {
                    if (sortingApplied)
                    {
                        return ((IOrderedQueryable<ItemDto>)query).ThenBy(x => x.Id);
                    }

                    return query.OrderBy(x => x.Id);
                });

            return ProjectItems(context);
        }
    }

    public sealed class ExpressionFieldQuery
    {
        [UseSorting<ItemDtoSortType>]
        public IQueryable<ItemDto> GetItems(ItemContext context)
            => ProjectItems(context);
    }

    public sealed class ItemDtoSortType : SortInputType<ItemDto>
    {
        protected override void Configure(ISortInputTypeDescriptor<ItemDto> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Name);
            descriptor.Field(x => 0 - x.Id).Name("reverseId");
        }
    }

    public sealed class ItemContext(DbContextOptions<ItemContext> options)
        : DbContext(options)
    {
        public DbSet<ItemEntity> Items { get; set; } = null!;
    }

    public sealed class ItemEntity
    {
        public int Id { get; set; }

        public required string Name { get; set; }
    }

    public sealed record ItemDto
    {
        public required int Id { get; init; }

        public required string Name { get; init; }
    }
}
