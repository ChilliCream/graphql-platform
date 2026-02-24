using System.Text.Json;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Sorting;

public sealed class QueryableSortVisitorDateTimeTests
{
    [Fact]
    public async Task Sort_Projected_DateTime_From_DateTimeOffset()
    {
        // arrange
        var databaseName = $"{Guid.NewGuid():N}.db";
        var executor = await new ServiceCollection()
            .AddDbContext<EventContext>(b => b.UseSqlite($"Data Source={databaseName}"))
            .AddGraphQL()
            .AddSorting()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                events(order: [{ timestamp: DESC }]) {
                    timestamp
                }
            }
            """);

        // assert
        using var document = JsonDocument.Parse(result.ToJson());
        Assert.False(
            document.RootElement.TryGetProperty("errors", out _),
            result.ToJson());

        var values = document.RootElement
            .GetProperty("data")
            .GetProperty("events")
            .EnumerateArray()
            .Select(t => DateTimeOffset.Parse(t.GetProperty("timestamp").GetString()!))
            .ToArray();

        var sorted = values.OrderByDescending(t => t).ToArray();
        Assert.Equal(sorted, values);
    }

    public sealed class Query
    {
        [UseSorting<EventSortType>]
        public IExecutable<ProjectedEvent> GetEvents(EventContext context)
        {
            SeedData(context);

            return context.Events
                .Select(x => new ProjectedEvent
                {
                    Timestamp = x.Timestamp.DateTime
                })
                .AsExecutable();
        }

        private static void SeedData(EventContext context)
        {
            context.Database.EnsureCreated();

            if (context.Events.Any())
            {
                return;
            }

            context.Events.AddRange(
                new EventEntity
                {
                    Timestamp = new DateTimeOffset(2025, 11, 14, 9, 0, 0, TimeSpan.FromHours(1))
                },
                new EventEntity
                {
                    Timestamp = new DateTimeOffset(2025, 11, 14, 14, 30, 0, TimeSpan.FromHours(1))
                },
                new EventEntity
                {
                    Timestamp = new DateTimeOffset(2025, 11, 15, 18, 0, 0, TimeSpan.FromHours(1))
                });

            context.SaveChanges();
        }
    }

    public sealed class EventContext(DbContextOptions<EventContext> options)
        : DbContext(options)
    {
        public DbSet<EventEntity> Events { get; set; } = null!;
    }

    public sealed class EventEntity
    {
        public int Id { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }

    public sealed record ProjectedEvent
    {
        public DateTime Timestamp { get; init; }
    }

    public sealed class EventSortType : SortInputType<ProjectedEvent>
    {
        protected override void Configure(ISortInputTypeDescriptor<ProjectedEvent> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(f => f.Timestamp);
        }
    }
}
