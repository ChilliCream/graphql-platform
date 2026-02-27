using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Issue5449Tests
{
    [Fact]
    public async Task UseProjection_Should_Keep_Entity_Service_Injection()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddDbContextPool<Issue5449Context>(
                    b => b.UseInMemoryDatabase($"Issue5449-{Guid.NewGuid():N}"))
                .AddGraphQL()
                .AddProjections()
                .AddQueryType<Issue5449Query>()
                .Services
                .BuildServiceProvider();

        var executor = await services
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync();

        await using (var scope = services.CreateAsyncScope())
        {
            await using var context = scope.ServiceProvider.GetRequiredService<Issue5449Context>();
            await context.Blogs.AddAsync(new Issue5449Blog("hc"));
            await context.SaveChangesAsync();
        }

        await using (var scope = services.CreateAsyncScope())
        {
            await using var context = scope.ServiceProvider.GetRequiredService<Issue5449Context>();
            var directMaterialization = await context.Blogs.AsNoTracking().SingleAsync();
            Assert.True(directMaterialization.ContextInjected);
        }

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              blogs {
                contextInjected
              }
            }
            """);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);

        using var document = JsonDocument.Parse(result.ToJson());
        var contextInjected = document
            .RootElement
            .GetProperty("data")
            .GetProperty("blogs")[0]
            .GetProperty("contextInjected")
            .GetBoolean();

        Assert.True(contextInjected);
    }

    public sealed class Issue5449Query
    {
        [UseProjection]
        public IQueryable<Issue5449Blog> GetBlogs(Issue5449Context context)
            => context.Blogs;
    }

    public sealed class Issue5449Context(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Issue5449Blog> Blogs => Set<Issue5449Blog>();
    }

    public sealed class Issue5449Blog
    {
        private readonly Issue5449Context? _context;

        public Issue5449Blog()
        {
            Name = string.Empty;
        }

        public Issue5449Blog(string name)
        {
            Name = name;
        }

        private Issue5449Blog(Issue5449Context context)
        {
            _context = context;
            Name = string.Empty;
        }

        public int Id { get; set; }

        public string Name { get; set; }

        [NotMapped]
        public bool ContextInjected => _context is not null;
    }
}
