using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Data.Sorting;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public sealed class DateTimeOffsetSortingTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task Sort_Projected_DateTime_From_DateTimeOffset()
    {
        // arrange
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);

        await using var services = new ServiceCollection()
            .AddDbContext<EventContext>(c => c.UseNpgsql(connectionString))
            .AddGraphQLServer()
            .AddSorting()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .AddQueryType<Query>()
            .Services
            .BuildServiceProvider();

        await using var scope = services.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<EventContext>();

        await context.Database.EnsureCreatedAsync();

        context.Events.AddRange(
            new EventEntity
            {
                Timestamp = new DateTimeOffset(2025, 11, 14, 8, 0, 0, TimeSpan.Zero)
            },
            new EventEntity
            {
                Timestamp = new DateTimeOffset(2025, 11, 14, 13, 30, 0, TimeSpan.Zero)
            },
            new EventEntity
            {
                Timestamp = new DateTimeOffset(2025, 11, 15, 17, 0, 0, TimeSpan.Zero)
            });

        await context.SaveChangesAsync();

        var executor = await services
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync();

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
            => context.Events
                .Select(x => new ProjectedEvent
                {
                    Timestamp = x.Timestamp.DateTime
                })
                .AsExecutable();
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
