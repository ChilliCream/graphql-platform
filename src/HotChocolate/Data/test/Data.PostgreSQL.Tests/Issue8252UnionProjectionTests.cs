using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public sealed class Issue8252UnionProjectionTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task Union_AllFragments_With_OffsetPaging_Should_Project()
    {
        // arrange
        var (executor, _) = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              contents {
                items {
                  ... on TextContent {
                    text
                  }
                  ... on ImageContent {
                    imageUrl
                  }
                }
              }
            }
            """);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "contents": {
                  "items": [
                    {
                      "imageUrl": "https://example.com/photo.jpg"
                    },
                    {
                      "text": "Hello World"
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Union_SingleFragment_With_OffsetPaging_Should_Project()
    {
        // arrange
        var (executor, _) = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              contents {
                items {
                  ... on TextContent {
                    text
                  }
                }
              }
            }
            """);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "contents": {
                  "items": [
                    null,
                    {
                      "text": "Hello World"
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Union_TypenameWithFragment_With_OffsetPaging_Should_Project()
    {
        // arrange
        var (executor, _) = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              contents {
                items {
                  __typename
                  ... on TextContent {
                    text
                  }
                }
              }
            }
            """);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "contents": {
                  "items": [
                    {
                      "__typename": "ImageContent"
                    },
                    {
                      "__typename": "TextContent",
                      "text": "Hello World"
                    }
                  ]
                }
              }
            }
            """);
    }

    private async Task<(IRequestExecutor Executor, ServiceProvider Services)> CreateExecutorAsync()
    {
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);

        var services = new ServiceCollection()
            .AddDbContext<Issue8252Context>(c => c.UseNpgsql(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Issue8252Query>()
            .AddUnionType<PostContent>()
            .AddType<TextContent>()
            .AddType<ImageContent>()
            .AddProjections()
            .AddFiltering()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .Services
            .BuildServiceProvider();

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<Issue8252Context>();
        await context.Database.EnsureCreatedAsync();

        context.Content.AddRange(
            new TextContent
            {
                Id = Guid.NewGuid(),
                Discriminator = "TEXT",
                Text = "Hello World"
            },
            new ImageContent
            {
                Id = Guid.NewGuid(),
                Discriminator = "IMAGE",
                ImageUrl = "https://example.com/photo.jpg",
                Height = 600
            });

        await context.SaveChangesAsync();

        var executor = await services
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync();

        return (executor, services);
    }

    public sealed class Issue8252Query
    {
        [UseOffsetPaging]
        [UseProjection]
        [UseFiltering]
        public IQueryable<PostContent> GetContents(Issue8252Context context)
            => context.Content;
    }

    public class PostContent
    {
        public Guid Id { get; set; }

        public required string Discriminator { get; set; }
    }

    public sealed class TextContent : PostContent
    {
        public required string Text { get; set; }
    }

    public sealed class ImageContent : PostContent
    {
        public required string ImageUrl { get; set; }

        public int Height { get; set; }
    }

    public sealed class Issue8252Context(DbContextOptions<Issue8252Context> options) : DbContext(options)
    {
        public DbSet<PostContent> Content => Set<PostContent>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<PostContent>().HasKey(e => e.Id);
            builder.Entity<PostContent>()
                .HasDiscriminator(e => e.Discriminator)
                .HasValue<TextContent>("TEXT")
                .HasValue<ImageContent>("IMAGE");
        }
    }
}
