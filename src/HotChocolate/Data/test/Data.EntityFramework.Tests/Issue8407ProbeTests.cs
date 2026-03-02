using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Issue8407ProbeTests
{
    [Fact]
    public async Task Sorting_With_Navigation_OrderBy_In_Projection_Does_Not_Throw()
    {
        IServiceProvider services =
            new ServiceCollection()
                .AddDbContextPool<Issue8407Context>(
                    b => b.UseInMemoryDatabase($"Issue8407-{Guid.NewGuid():N}"))
                .AddGraphQL()
                .AddSorting()
                .AddQueryType<Issue8407Query>()
                .Services
                .BuildServiceProvider();

        await using (var scope = services.CreateAsyncScope())
        {
            await using var db = scope.ServiceProvider.GetRequiredService<Issue8407Context>();

            db.Parents.Add(
                new Issue8407Parent
                {
                    Id = 2,
                    Children =
                    [
                        new Issue8407Child { Id = 21, SomeDate = new DateTime(2025, 1, 2) },
                        new Issue8407Child { Id = 22, SomeDate = new DateTime(2025, 1, 1) }
                    ]
                });

            db.Parents.Add(
                new Issue8407Parent
                {
                    Id = 1,
                    Children =
                    [
                        new Issue8407Child { Id = 11, SomeDate = new DateTime(2025, 1, 3) }
                    ]
                });

            await db.SaveChangesAsync();
        }

        var executor = await services.GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              parents(order: [{ id: ASC }]) {
                id
                children {
                  id
                }
              }
            }
            """);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
    }

    public sealed class Issue8407Query
    {
        [UseSorting]
        public IQueryable<Issue8407Parent> GetParents(Issue8407Context db) =>
            db.Parents
                .Select(p => new Issue8407Parent
                {
                    Id = p.Id,
                    Children = p.Children
                        .AsQueryable()
                        .OrderBy(c => c.SomeDate)
                        .ToList()
                });
    }

    public sealed class Issue8407Context(DbContextOptions<Issue8407Context> options) : DbContext(options)
    {
        public DbSet<Issue8407Parent> Parents => Set<Issue8407Parent>();

        public DbSet<Issue8407Child> Children => Set<Issue8407Child>();
    }

    public sealed class Issue8407Parent
    {
        public int Id { get; set; }

        public List<Issue8407Child> Children { get; set; } = [];
    }

    public sealed class Issue8407Child
    {
        public int Id { get; set; }

        public DateTime SomeDate { get; set; }
    }
}
